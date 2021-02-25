namespace FM.LiveSwitch.Hammer
{
    class CapacityThresholds
    {
        public bool Enabled { get; set; }

        public int? McuConnectionsPerCpuThreshold { get; set; }

        public int? SfuConnectionsPerCpuThreshold { get; set; }

        public int? SafeMcuConnectionsPerCpuThreshold { get; set; }

        public int? SafeSfuConnectionsPerCpuThreshold { get; set; }

        public int? UnsafeMcuConnectionsPerCpuThreshold { get; set; }

        public int? UnsafeSfuConnectionsPerCpuThreshold { get; set; }
    }
}
