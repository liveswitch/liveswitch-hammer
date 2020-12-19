using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace FM.LiveSwitch.Hammer
{
    enum ScanTestMode
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

    static class ScanTestModeExtensions
    {
        public static string ToDisplayString(this ScanTestMode mode)
        {
            return typeof(ScanTestMode).GetMember(mode.ToString()).First().GetCustomAttribute<DisplayAttribute>().Name;
        }

        public static bool IsCompatible(this ScanTestMode mode, IceServer iceServer)
        {
            if (mode == ScanTestMode.Stun)
            {
                return iceServer.IsStun;
            }
            if (mode == ScanTestMode.TurnUdp)
            {
                return iceServer.IsTurn && iceServer.IsUdp;
            }
            if (mode == ScanTestMode.TurnTcp)
            {
                return iceServer.IsTurn && iceServer.IsTcp && !iceServer.IsSecure;
            }
            if (mode == ScanTestMode.Turns) 
            {
                return iceServer.IsTurn && iceServer.IsTcp && iceServer.IsSecure;
            }
            return false;
        }
    }
}
