using CommandLine;

namespace FM.LiveSwitch.Hammer
{
    [Verb("cluster", HelpText = "Tests scenarios that evoke clustering edge cases.")]
    class ClusterTestOptions : Options
    {
        [Option("iteration-count", Default = 1000, HelpText = "The number of iterations to run.")]
        public int IterationCount { get; set; }

        [Option("media-timeout", Default = 5, HelpText = "The number of seconds to wait for media to flow.")]
        public int MediaTimeout { get; set; }

        [Option("user1", Default = "user1", HelpText = "The user ID of the first client.")]
        public string User1 { get; set; }

        [Option("user2", Default = "user2", HelpText = "The user ID of the second client.")]
        public string User2 { get; set; }

        [Option("device1", Default = "device1", HelpText = "The device ID of the first client.")]
        public string Device1 { get; set; }

        [Option("device2", Default = "device2", HelpText = "The device ID of the second client.")]
        public string Device2 { get; set; }

        [Option("region1", Default = "region1", HelpText = "The region of the first client.")]
        public string Region1 { get; set; }

        [Option("region2", Default = "region2", HelpText = "The region of the second client.")]
        public string Region2 { get; set; }
    }
}
