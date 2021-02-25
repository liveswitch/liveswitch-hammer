using Newtonsoft.Json;

namespace FM.LiveSwitch.Hammer
{
    class MediaServerInfo
    {
        public string Id { get; set; }

        public string Region { get; set; }

        public bool Available { get; set; }

        public bool Active { get; set; }

        public bool OverCapacity { get; set; }

        public double UsedCapacity { get; set; }

        public bool Draining { get; set; }

        public string[] IPAddresses { get; set; }

        public string PublicIPAddress { get; set; }

        public string Version { get; set; }

        public int CoreCount { get; set; }

        [JsonIgnore]
        public string DeploymentId { get; set; }
    }
}
