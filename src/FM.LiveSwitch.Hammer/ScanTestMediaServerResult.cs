using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FM.LiveSwitch.Hammer
{
    class ScanTestMediaServerResult
    {
        public string MediaServerId { get; private set; }

        public ScanTestState State { get; private set; }

        public Exception Exception { get; private set; }

        public string Reason { get; private set; }

        public ScanTestScenarioResult[] ScenarioResults { get { return _ScenarioResults.Select(x => x.Value).ToArray();  } }

        [JsonIgnore]
        public ScanTestScenarioResult[] PassedScenarioResults { get { return _ScenarioResults.Where(x => x.Value.State == ScanTestState.Pass).Select(x => x.Value).ToArray(); } }

        [JsonIgnore]
        public ScanTestScenarioResult[] FailedScenarioResults { get { return _ScenarioResults.Where(x => x.Value.State == ScanTestState.Fail).Select(x => x.Value).ToArray(); } }

        [JsonIgnore]
        public ScanTestScenarioResult[] SkippedScenarioResults { get { return _ScenarioResults.Where(x => x.Value.State == ScanTestState.Skip).Select(x => x.Value).ToArray(); } }

        [JsonIgnore]
        public ScanTestScenarioResult[] UnknownScenarioResults { get { return _ScenarioResults.Where(x => x.Value.State == ScanTestState.Unknown).Select(x => x.Value).ToArray(); } }

        private readonly Dictionary<ScanTestScenario, ScanTestScenarioResult> _ScenarioResults;

        private ScanTestMediaServerResult(string mediaServerId)
        {
            MediaServerId = mediaServerId;

            _ScenarioResults = new Dictionary<ScanTestScenario, ScanTestScenarioResult>();
        }

        public static ScanTestMediaServerResult Unknown(string mediaServerId)
        {
            return new ScanTestMediaServerResult(mediaServerId)
            {
                State = ScanTestState.Unknown
            };
        }

        public static ScanTestMediaServerResult Pass(string mediaServerId)
        {
            return new ScanTestMediaServerResult(mediaServerId)
            {
                State = ScanTestState.Pass
            };
        }

        public static ScanTestMediaServerResult Fail(string mediaServerId, Exception exception)
        {
            return new ScanTestMediaServerResult(mediaServerId)
            {
                State = ScanTestState.Fail,
                Reason = exception.Message,
                Exception = exception
            };
        }

        public static ScanTestMediaServerResult Skip(string mediaServerId, string reason)
        {
            return new ScanTestMediaServerResult(mediaServerId)
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
            if (scenarioResult.State == ScanTestState.Fail)
            {
                State = ScanTestState.Fail;
                Exception = scenarioResult.Exception;
                Reason = Exception.Message;
            }

            _ScenarioResults[scenario] = scenarioResult;
        }

        public bool IsCertificateExpiring(TimeSpan maxCertificateValidFor)
        {
            return _ScenarioResults.Values.Any(x => x.CertificateValidFor != null && x.CertificateValidFor < maxCertificateValidFor);
        }
    }
}
