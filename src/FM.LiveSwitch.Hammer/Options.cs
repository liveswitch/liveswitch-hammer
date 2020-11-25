using CommandLine;

namespace FM.LiveSwitch.Hammer
{
    abstract class Options
    {
        [Option('g', "gateway-url", Default = "http://localhost:8080/sync", HelpText = "The Gateway URL.")]
        public string GatewayUrl { get; set; }

        [Option('a', "application-id", Default = "my-app-id", HelpText = "The application identifier.")]
        public string ApplicationId { get; set; }

        [Option('s', "shared-secret", Default = "--replaceThisWithYourOwnSharedSecret--", HelpText = "The shared secret.")]
        public string SharedSecret { get; set; }
    }
}
