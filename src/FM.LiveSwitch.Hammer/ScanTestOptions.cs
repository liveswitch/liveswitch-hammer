using CommandLine;

namespace FM.LiveSwitch.Hammer
{
    [Verb("scan", HelpText = "Tests individual Media Servers for connectivity and media flow.")]
    class ScanTestOptions : Options
    {
        [Option("api-key", Required = true, HelpText = "The API key to authenticate requests.")]
        public string ApiKey { get; set; }

        [Option("api-base-url", Default = "http://localhost:9090/admin/api", HelpText = "The base URL of the Gateway API service.")]
        public string ApiBaseUrl { get; set; }

        [Option("no-host", Default = false, HelpText = "Do not test host-only connectivity.")]
        public bool NoHost { get; set; }

        [Option("no-stun", Default = false, HelpText = "Do not test STUN-only connectivity.")]
        public bool NoStun { get; set; }

        [Option("no-turn-udp", Default = false, HelpText = "Do not test TURN/UDP-only connectivity.")]
        public bool NoTurnUdp { get; set; }

        [Option("no-turn-tcp", Default = false, HelpText = "Do not test TURN/TCP-only connectivity.")]
        public bool NoTurnTcp { get; set; }

        [Option("no-turns", Default = false, HelpText = "Do not test TURNS-only connectivity.")]
        public bool NoTurns { get; set; }

        [Option("min-cert-days", Default = 7, HelpText = "The minimum number of days to allow until certificate expiry.")]
        public int MinCertDays { get; set; }

        [Option("max-attempts", Default = 3, HelpText = "The max number of attempts for a given Media Server.")]
        public int MaxAttempts { get; set; }

        [Option("attempt-interval", Default = 5, HelpText = "The interval, in seconds, between failed attempts.")]
        public int AttemptInterval { get; set; }

        [Option("media-timeout", Default = 5, HelpText = "The number of seconds to wait for media to flow.")]
        public int MediaTimeout { get; set; }

        [Option("tag", Default = null, HelpText = "The client's tag.")]
        public string Tag { get; set; }

        [Option("media-server-id", Default = null, HelpText = "The Media Server to test.")]
        public string MediaServerId { get; set; }

        public bool ShouldTest(string mediaServerId)
        {
            if (!string.IsNullOrEmpty(MediaServerId))
            {
                return MediaServerId == mediaServerId;
            }
            return true;
        }

        public bool ShouldTest(ScanTestScenario scenario)
        {
            if ((NoHost && scenario == ScanTestScenario.Host) ||
                (NoStun && scenario == ScanTestScenario.Stun) ||
                (NoTurnUdp && scenario == ScanTestScenario.TurnUdp) ||
                (NoTurnTcp && scenario == ScanTestScenario.TurnTcp) ||
                (NoTurns && scenario == ScanTestScenario.Turns))
            {
                return false;
            }
            return true;
        }
    }
}
