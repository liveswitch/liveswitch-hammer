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

        [Option("tag-1", Default = null, HelpText = "The first client's tag.")]
        public string Tag1 { get; set; }

        [Option("tag-2", Default = null, HelpText = "The second client's tag.")]
        public string Tag2 { get; set; }

        [Option("region1", Default = null, HelpText = "The first client's region.")]
        public string Region1 { get; set; }

        [Option("region2", Default = null, HelpText = "The second client's region.")]
        public string Region2 { get; set; }
    }
}
