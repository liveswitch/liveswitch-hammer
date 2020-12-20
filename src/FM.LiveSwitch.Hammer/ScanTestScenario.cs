using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace FM.LiveSwitch.Hammer
{
    enum ScanTestScenario
    {
        [Display(Name = "Host")]
        Host,
        [Display(Name = "STUN")]
        Stun,
        [Display(Name = "TURN/UDP")]
        TurnUdp,
        [Display(Name = "TURN/TCP")]
        TurnTcp,
        [Display(Name = "TURNS")]
        Turns
    }

    static class ScanTestScenarioExtensions
    {
        public static string ToDisplayString(this ScanTestScenario scenario)
        {
            return typeof(ScanTestScenario).GetMember(scenario.ToString()).First().GetCustomAttribute<DisplayAttribute>().Name;
        }

        public static bool IsCompatible(this ScanTestScenario scenario, IceServer iceServer)
        {
            if (scenario == ScanTestScenario.Stun)
            {
                return iceServer.IsStun;
            }
            if (scenario == ScanTestScenario.TurnUdp)
            {
                return iceServer.IsTurn && iceServer.IsUdp;
            }
            if (scenario == ScanTestScenario.TurnTcp)
            {
                return iceServer.IsTurn && iceServer.IsTcp && !iceServer.IsSecure;
            }
            if (scenario == ScanTestScenario.Turns) 
            {
                return iceServer.IsTurn && iceServer.IsTcp && iceServer.IsSecure;
            }
            return false;
        }

        public static bool RequiresTls(this ScanTestScenario scenario)
        {
            return scenario == ScanTestScenario.Turns;
        }
    }
}
