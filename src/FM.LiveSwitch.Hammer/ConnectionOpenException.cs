using System;

namespace FM.LiveSwitch.Hammer
{
    [Serializable]
    public class ConnectionOpenException : Exception
    {
        public ConnectionOpenException(string message, Exception innerException) 
            : base(message, innerException)
        { }
    }
}