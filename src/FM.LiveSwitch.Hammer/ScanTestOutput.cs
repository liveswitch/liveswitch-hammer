using System.Collections.Generic;

namespace FM.LiveSwitch.Hammer
{
    class ScanTestOutput
    {
        public IEnumerable<ScanTestMediaServerResult> Failed { get; init; }
        public IEnumerable<ScanTestMediaServerResult> Expiring { get; init; }
    }
}
