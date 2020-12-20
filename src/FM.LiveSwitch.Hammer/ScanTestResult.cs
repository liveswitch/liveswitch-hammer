using System.Collections.Generic;
using System.Linq;

namespace FM.LiveSwitch.Hammer
{
    class ScanTestResult
    {
        public string MediaServerId { get; private set; }

        public ScanTestScenarioResult[] ScenarioResults { get { return _ScenarioResults.Select(x => x.Value).ToArray(); } }

        private readonly Dictionary<ScanTestScenario, ScanTestScenarioResult> _ScenarioResults;

        public ScanTestResult(string mediaServerId)
        {
            MediaServerId = mediaServerId;

            _ScenarioResults = new Dictionary<ScanTestScenario, ScanTestScenarioResult>();
        }

        public ScanTestScenarioResult GetScenarioResult(ScanTestScenario scenario)
        {
            if (_ScenarioResults.TryGetValue(scenario, out var scenarioResult))
            {
                return scenarioResult;
            }
            return new ScanTestScenarioResult();
        }

        public void SetScenarioResult(ScanTestScenario scenario, ScanTestScenarioResult scenarioResult)
        {
            _ScenarioResults[scenario] = scenarioResult;
        }
    }
}
