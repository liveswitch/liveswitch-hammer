using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace FM.LiveSwitch.Hammer
{
    class ScanTest
    {
        public ScanTestOptions Options { get; private set; }

        public ConcurrentDictionary<string, X509Certificate2> ServerCertificates { get; private set; }

        private HttpClient _HttpClient;

        public ScanTest(ScanTestOptions options)
        {
            Options = options;
            ServerCertificates = new ConcurrentDictionary<string, X509Certificate2>();

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

                mediaServerResults.Add(await Scan(mediaServer, cancellationToken).ConfigureAwait(false));
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

        private async Task<ScanTestMediaServerResult> Scan(MediaServerInfo mediaServer, CancellationToken cancellationToken)
        {
            Exception exception = null;
            for (var i = 0; i < Options.MaxAttempts; i++)
            {
                // delay between attempts
                if (i > 0)
                {
                    await Task.Delay(Options.AttemptInterval * Constants.MillisecondsPerSecond).ConfigureAwait(false);
                }

                Console.Error.WriteLine();
                Console.Error.WriteLine($"Test Attempt #{i + 1}");

                // don't test inactive Media Servers
                if (!mediaServer.Active)
                {
                    Console.Error.WriteLine("Media Server is inactive. Skipping...");
                    return ScanTestMediaServerResult.Skip(mediaServer.Id, "Media Server is inactive.");
                }

                // don't test draining Media Servers
                if (mediaServer.Draining)
                {
                    Console.Error.WriteLine("Media Server is draining. Skipping...");
                    return ScanTestMediaServerResult.Skip(mediaServer.Id, "Media Server is draining.");
                }

                // don't test over-capacity Media Servers
                if (mediaServer.OverCapacity)
                {
                    Console.Error.WriteLine("Media Server is over-capacity. Skipping...");
                    return ScanTestMediaServerResult.Skip(mediaServer.Id, "Media Server is over-capacity.");
                }

                try
                {
                    // test Media Server
                    var test = new ScanTestMediaServer(Options);
                    var result = await test.Run(mediaServer.Id, cancellationToken).ConfigureAwait(false);

                    // merge server certificates
                    foreach (var (targetHost, remoteCertificate) in test.ServerCertificates)
                    {
                        ServerCertificates.TryAdd(targetHost, remoteCertificate);
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    // don't test unregistered Media Servers
                    if (ex is MediaServerMismatchException &&
                        await GetMediaServer(mediaServer.Id).ConfigureAwait(false) == null)
                    {
                        return ScanTestMediaServerResult.Skip(mediaServer.Id, "Media Server has unregistered.");
                    }

                    exception = ex;
                }
            }

            // retries exhausted
            return ScanTestMediaServerResult.Fail(mediaServer.Id, exception);
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

        private async Task<MediaServerInfo[]> GetMediaServers()
        {
            var responseJson = await _HttpClient.GetStringAsync("v1/mediaservers").ConfigureAwait(false);
            return JsonConvert.DeserializeObject<MediaServerInfo[]>(responseJson);
        }

        private async Task<MediaServerInfo> GetMediaServer(string mediaServerId)
        {
            return (await GetMediaServers().ConfigureAwait(false)).FirstOrDefault(mediaServer => mediaServer.Id == mediaServerId);
        }
    }
}
