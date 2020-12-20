using System;

namespace FM.LiveSwitch.Hammer
{
    class ScanTestScenarioResult
    {
        public ScanTestScenarioResultState State { get; set; }

        public TimeSpan? CertificateExpiry { get; set; }

        public Exception Exception { get; set; }

        public ScanTestScenarioResult()
            : this(ScanTestScenarioResultState.Unknown)
        { }

        public ScanTestScenarioResult(ScanTestScenarioResultState state)
        {
            State = state;
        }
    }
}
