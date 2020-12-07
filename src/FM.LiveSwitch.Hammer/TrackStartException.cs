using System;

namespace FM.LiveSwitch.Hammer
{
    [Serializable]
    public class TrackStartException : Exception
    {
        public TrackStartException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}