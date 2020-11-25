using CommandLine;

namespace FM.LiveSwitch.Hammer
{
    [Verb("load", HelpText = "Tests parallel and sequential load scenarios.")]
    class LoadTestOptions : Options
    {
        [Option("iteration-count", Default = 1, HelpText = "The number of iterations to run.")]
        public int IterationCount { get; set; }

        [Option("client-count", Default = 1, HelpText = "The number of clients to register.")]
        public int ClientCount { get; set; }

        [Option("channel-count", Default = 1, HelpText = "The number of channels for each client to join.")]
        public int ChannelCount { get; set; }

        [Option("connection-count", Default = 1, HelpText = "The number of connections in each channel to open.")]
        public int ConnectionCount { get; set; }

        [Option("parallel-client-registers", Default = 1, HelpText = "The number of parallel client register operations.")]
        public int ParallelClientRegisters { get; set; }

        [Option("parallel-channel-joins", Default = 1, HelpText = "The number of parallel channel join operations.")]
        public int ParallelChannelJoins { get; set; }

        [Option("parallel-connection-opens", Default = 1, HelpText = "The number of parallel connection open operations.")]
        public int ParallelConnectionOpens { get; set; }

        [Option("channel-burst", Default = false, HelpText = "Group traffic bursts by channel.")]
        public bool ChannelBurst { get; set; }

        [Option("pause-timeout", Default = 0, HelpText = "The number of seconds to wait before closing connections.")]
        public int PauseTimeout { get; set; }
    }
}
