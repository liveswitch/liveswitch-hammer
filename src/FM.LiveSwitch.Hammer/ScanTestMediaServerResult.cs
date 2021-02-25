using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FM.LiveSwitch.Hammer
{
    class ScanTestMediaServerResult : ScanTestResult
    {
        public MediaServerInfo MediaServer { get; private set; }

        public ScanTestScenarioResult[] Results { get { return _ScenarioResults.Select(x => x.Value).ToArray();  } }

        [JsonIgnore]
        public ScanTestScenarioResult[] PassedResults { get { return _ScenarioResults.Where(x => x.Value.State == ScanTestState.Pass).Select(x => x.Value).ToArray(); } }

        [JsonIgnore]
        public ScanTestScenarioResult[] FailedResults { get { return _ScenarioResults.Where(x => x.Value.State == ScanTestState.Fail).Select(x => x.Value).ToArray(); } }

        [JsonIgnore]
        public ScanTestScenarioResult[] SkippedResults { get { return _ScenarioResults.Where(x => x.Value.State == ScanTestState.Skip).Select(x => x.Value).ToArray(); } }

        [JsonIgnore]
        public ScanTestScenarioResult[] UnknownResults { get { return _ScenarioResults.Where(x => x.Value.State == ScanTestState.Unknown).Select(x => x.Value).ToArray(); } }

        private readonly Dictionary<ScanTestScenario, ScanTestScenarioResult> _ScenarioResults;

        private ScanTestMediaServerResult(MediaServerInfo mediaServer)
        {
            MediaServer = mediaServer;

            _ScenarioResults = new Dictionary<ScanTestScenario, ScanTestScenarioResult>();
        }

        public static ScanTestMediaServerResult Unknown(MediaServerInfo mediaServer)
        {
            return new ScanTestMediaServerResult(mediaServer)
            {
                State = ScanTestState.Unknown
            };
        }

        public static ScanTestMediaServerResult Pass(MediaServerInfo mediaServer)
        {
            return new ScanTestMediaServerResult(mediaServer)
            {
                State = ScanTestState.Pass
            };
        }

        public static ScanTestMediaServerResult Fail(MediaServerInfo mediaServer, Exception exception)
        {
            return new ScanTestMediaServerResult(mediaServer)
            {
                State = ScanTestState.Fail,
                Exception = exception
            };
        }

        public static ScanTestMediaServerResult Skip(MediaServerInfo mediaServer, string reason)
        {
            return new ScanTestMediaServerResult(mediaServer)
            {
                State = ScanTestState.Skip,
                Reason = reason
            };
        }

        public ScanTestScenarioResult GetScenarioResult(ScanTestScenario scenario)
        {
            if (_ScenarioResults.TryGetValue(scenario, out var scenarioResult))
            {
                return scenarioResult;
            }
            return ScanTestScenarioResult.Unknown(scenario);
        }

        public void SetScenarioResult(ScanTestScenario scenario, ScanTestScenarioResult scenarioResult)
        {
            _ScenarioResults[scenario] = scenarioResult;

            if (scenarioResult.State == ScanTestState.Fail)
            {
                State = ScanTestState.Fail;
                Reason = $"{FailedResults.Length} scenario(s) failed.";
            }
        }

        public bool IsCertificateExpiring(TimeSpan maxCertificateValidFor)
        {
            return _ScenarioResults.Values.Any(x => x.CertificateValidFor != null && x.CertificateValidFor < maxCertificateValidFor);
        }
    }
}
