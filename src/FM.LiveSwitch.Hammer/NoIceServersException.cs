using System;

namespace FM.LiveSwitch.Hammer
{
    [Serializable]
    public class NoIceServersException : Exception
    {
        public NoIceServersException(string message)
            : base(message)
        { }
    }
}