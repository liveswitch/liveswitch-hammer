using System;

namespace FM.LiveSwitch.Hammer
{
    [Serializable]
    public class ClientRegisterException : Exception
    {
        public ClientRegisterException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}