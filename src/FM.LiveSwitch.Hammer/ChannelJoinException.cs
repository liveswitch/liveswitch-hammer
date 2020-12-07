using System;

namespace FM.LiveSwitch.Hammer
{
    [Serializable]
    public class ChannelJoinException : Exception
    {
        public ChannelJoinException(string message, Exception innerException) 
            : base(message, innerException)
        { }
    }
}