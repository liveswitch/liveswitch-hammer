using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
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
            for (var i = 0; i < mediaServers.Length; i++)
            {
                var mediaServer = mediaServers[i];

                Console.Error.WriteLine();
                Console.Error.WriteLine($"Media Server {mediaServer.Id} ({i + 1}/{mediaServers.Length})");

                await Scan(mediaServer, cancellationToken).ConfigureAwait(false);
            }

            // check for expiring certificates
            CheckServerCertificates();
        }

        private void CheckServerCertificates()
        {
            var now = DateTime.UtcNow;
            foreach (var (targetHost, remoteCertificate) in ServerCertificates)
            {
                var remoteCertificateRemaining = remoteCertificate.NotAfter - now;
                if (remoteCertificateRemaining < TimeSpan.FromDays(Options.MinCertDays))
                {
                    string expiresString;
                    if (remoteCertificateRemaining.TotalDays > 1)
                    {
                        expiresString = $"in {(int)remoteCertificateRemaining.TotalDays} day(s)";
                    }
                    else if (remoteCertificateRemaining.TotalHours > 1)
                    {
                        expiresString = $"in {(int)remoteCertificateRemaining.TotalHours} hour(s)";
                    }
                    else if (remoteCertificateRemaining.TotalMinutes > 1)
                    {
                        expiresString = $"in {(int)remoteCertificateRemaining.TotalMinutes} minute(s)";
                    }
                    else
                    {
                        expiresString = "momentarily";
                    }
                    throw new CertificateExpiringException($"Certificate for target host '{targetHost}' expires {expiresString}.", remoteCertificate);
                }
            }
        }

        private async Task<bool> Scan(MediaServerInfo mediaServer, CancellationToken cancellationToken)
        {
            for (var i = 0; i < Options.MaxAttempts; i++)
            {
                // delay between attempts
                if (i > 0)
                {
                    await Task.Delay(Options.AttemptInterval * Constants.MillisecondsPerSecond).ConfigureAwait(false);
                }

                Console.Error.WriteLine();
                Console.Error.WriteLine($"Test #{i + 1}");

                // don't test inactive Media Servers
                if (!mediaServer.Active)
                {
                    Console.Error.WriteLine("Media Server is inactive. Skipping...");
                    return false;
                }

                // don't test draining Media Servers
                if (mediaServer.Draining)
                {
                    Console.Error.WriteLine("Media Server is draining. Skipping...");
                    return false;
                }

                // don't test over-capacity Media Servers
                if (mediaServer.OverCapacity)
                {
                    Console.Error.WriteLine("Media Server is over-capacity. Skipping...");
                    return false;
                }

                try
                {
                    // create iteration
                    var iteration = new ScanTestIteration(Options);

                    // run iteration
                    await iteration.Run(mediaServer.Id, cancellationToken).ConfigureAwait(false);

                    // merge server certificates
                    foreach (var (targetHost, remoteCertificate) in iteration.ServerCertificates)
                    {
                        ServerCertificates.TryAdd(targetHost, remoteCertificate);
                    }
                    return true;
                }
                catch (MediaServerMismatchException)
                {
                    // check if Media Server is still present
                    mediaServer = await GetMediaServer(mediaServer.Id).ConfigureAwait(false);
                    if (mediaServer == null)
                    {
                        Console.Error.WriteLine("Media Server has unregistered.");
                        return false;
                    }
                }
            }
            return false;
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
