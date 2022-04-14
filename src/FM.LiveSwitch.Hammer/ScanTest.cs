using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FM.LiveSwitch.Hammer
{
    class ScanTest : Test
    {
        public ScanTestOptions Options { get; private set; }

        private HttpClient _HttpClient;

        public ScanTest(ScanTestOptions options)
            : base(options)
        {
            Options = options;

            if (Options.MinCertDays < 0)
            {
                throw new ArgumentException("--min-cert-days cannot be negative.");
            }

            InitializeHttpClient();
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            // scan each Media Server
            var mediaServers = await GetMediaServers().ConfigureAwait(false);
            var mediaServerResults = new List<ScanTestMediaServerResult>();
            for (var i = 0; i < mediaServers.Length; i++)
            {
                var mediaServer = mediaServers[i];

                Console.Error.WriteLine();
                Console.Error.WriteLine($"Media Server {mediaServer.Id} ({i + 1}/{mediaServers.Length})");

                mediaServerResults.Add(await Scan(mediaServer.Id, cancellationToken).ConfigureAwait(false));
            }

            // check if any Media Servers failed
            var failedMediaServerResults = mediaServerResults.Where(x => x.State == ScanTestState.Fail);
            if (failedMediaServerResults.Any())
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Some Media Servers failed at least one scenario.");
            }

            // check if any Media Servers have expiring certificates
            var expiringMediaServerResults = mediaServerResults.Where(x => x.IsCertificateExpiring(TimeSpan.FromDays(Options.MinCertDays)));
            if (expiringMediaServerResults.Any())
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Some Media Servers have expiring certificates.");
            }

            if (!failedMediaServerResults.Any() && !expiringMediaServerResults.Any())
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("All Media Servers passed all scenarios.");
            }

            Console.Error.WriteLine();
            Console.Error.WriteLine("Writing test results to standard output...");
            Console.WriteLine(JsonConvert.SerializeObject(new
            {
                failed = failedMediaServerResults,
                expiring = expiringMediaServerResults
            }));
        }

        private async Task<ScanTestMediaServerResult> Scan(string mediaServerId, CancellationToken cancellationToken)
        {
            var attempt = 1;
            while (true)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine($"Test Attempt #{attempt}");

                // get current info
                var mediaServer = await GetMediaServer(mediaServerId).ConfigureAwait(false);
                if (mediaServer == null)
                {
                    Console.Error.WriteLine("Media Server is gone. Skipping...");
                    return ScanTestMediaServerResult.Skip(mediaServer, "Media Server is gone.");
                }

                // don't test inactive Media Servers
                if (!mediaServer.Active)
                {
                    Console.Error.WriteLine("Media Server is inactive. Skipping...");
                    return ScanTestMediaServerResult.Skip(mediaServer, "Media Server is inactive.");
                }

                // don't test draining Media Servers
                if (mediaServer.Draining)
                {
                    Console.Error.WriteLine("Media Server is draining. Skipping...");
                    return ScanTestMediaServerResult.Skip(mediaServer, "Media Server is draining.");
                }

                // don't test over-capacity Media Servers
                if (mediaServer.OverCapacity)
                {
                    Console.Error.WriteLine("Media Server is over-capacity. Skipping...");
                    return ScanTestMediaServerResult.Skip(mediaServer, "Media Server is over-capacity.");
                }

                // scan Media Server
                var result = await DoScan(mediaServer, cancellationToken).ConfigureAwait(false);

                // check for success
                if (result.State != ScanTestState.Fail)
                {
                    return result;
                }

                // check for acceptable failures
                if (result.Exception is MediaServerMismatchException)
                {
                    if (await MediaServerIsGone(mediaServer.Id).ConfigureAwait(false))
                    {
                        Console.Error.WriteLine("Media Server has unregistered. Skipping...");
                        return ScanTestMediaServerResult.Skip(mediaServer, "Media Server has unregistered.");
                    }
                    else if (await MediaServerWouldBeOverCapacity(mediaServer.Id).ConfigureAwait(false))
                    {
                        Console.Error.WriteLine("Media Server would be over-capacity. Skipping...");
                        return ScanTestMediaServerResult.Skip(mediaServer, "Media Server would be over-capacity.");
                    }
                }

                // check for max attempts
                if (attempt >= Options.MaxAttempts)
                {
                    return result;
                }

                attempt++;

                // time to try again
                await Task.Delay(Options.AttemptInterval * Constants.MillisecondsPerSecond).ConfigureAwait(false);
            }
        }

        private Task<ScanTestMediaServerResult> DoScan(MediaServerInfo mediaServer, CancellationToken cancellationToken)
        {
            try
            {
                return new ScanTestMediaServer(Options).Run(mediaServer, cancellationToken);
            }
            catch (Exception ex)
            {
                return Task.FromResult(ScanTestMediaServerResult.Fail(mediaServer, ex));
            }
        }

        private void InitializeHttpClient()
        {
            if (!Uri.TryCreate(Options.ApiBaseUrl.TrimEnd('/') + '/', UriKind.Absolute, out var apiBaseUri))
            {
                throw new ArgumentException("Invalid API base URL.");
            }

            _HttpClient = new HttpClient
            {
                BaseAddress = apiBaseUri
            };

            _HttpClient.DefaultRequestHeaders.Add("X-API-Key", Options.ApiKey);
        }

        private async Task<bool> MediaServerIsGone(string mediaServerId)
        {
            return await GetMediaServer(mediaServerId).ConfigureAwait(false) == null;
        }

        private async Task<bool> MediaServerWouldBeOverCapacity(string mediaServerId)
        {
            var mediaServer = await GetMediaServer(mediaServerId).ConfigureAwait(false);
            if (mediaServer == null)
            {
                return false;
            }

            var deploymentConfig = await GetDeploymentConfig(mediaServer.DeploymentId).ConfigureAwait(false);
            if (deploymentConfig == null)
            {
                return false;
            }

            var capacityThresholds = deploymentConfig.CapacityThresholds;
            if (capacityThresholds == null)
            {
                return false;
            }

            var sfuConnectionsPerCpuThreshold =
                capacityThresholds.SfuConnectionsPerCpuThreshold ?? 
                capacityThresholds.SafeSfuConnectionsPerCpuThreshold ?? 
                capacityThresholds.UnsafeSfuConnectionsPerCpuThreshold;
            if (sfuConnectionsPerCpuThreshold == null || sfuConnectionsPerCpuThreshold <= 0)
            {
                return false;
            }

            var sfuUsedCapacityIncrement = 1.0 / sfuConnectionsPerCpuThreshold * mediaServer.CoreCount;
            if (sfuUsedCapacityIncrement + mediaServer.UsedCapacity > 1.0)
            {
                return true;
            }

            return false;
        }

        private async Task<MediaServerInfo[]> GetMediaServers()
        {
            string responseJson = string.Empty;

            var count = 0;
            var delay = 100;
            while(true)//we either break out of the loop, or throw an exception
            {
                try
                {
                    responseJson = await _HttpClient.GetStringAsync("v1/mediaservers").ConfigureAwait(false);
                    break;
                }
                catch(System.Net.Http.HttpRequestException ex)
                {
                    if(count <= 5)
                    {
                        Log.Warn("Unable to retrieve media servers; retrying...", ex);
                        await Task.Delay(delay).ConfigureAwait(false);

                        count++;
                        delay *= 2;
                        
                    }
                    else
                    {
                        Log.Error("Unable to retrieve media servers; retry count exceeded.", ex);
                        throw;
                    }
                }    
            }
            
            var mediaServers = JsonConvert.DeserializeObject<MediaServerInfo[]>(responseJson);
            return mediaServers.Where(mediaServer => Options.ShouldTest(mediaServer.Id)).ToArray();
        }

        private async Task<MediaServerInfo> GetMediaServer(string mediaServerId)
        {
            return (await GetMediaServers().ConfigureAwait(false)).FirstOrDefault(mediaServer => mediaServer.Id == mediaServerId);
        }

        private async Task<DeploymentConfig> GetDeploymentConfig(string deploymentId)
        {
            var responseJson = await _HttpClient.GetStringAsync($"v2/DeploymentConfig({deploymentId})").ConfigureAwait(false);
            return JsonConvert.DeserializeObject<DeploymentConfig>(responseJson);
        }
    }
}
