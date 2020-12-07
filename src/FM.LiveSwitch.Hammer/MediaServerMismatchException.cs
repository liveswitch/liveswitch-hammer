using System;

namespace FM.LiveSwitch.Hammer
{
    [Serializable]
    public class MediaServerMismatchException : Exception
    {
        public MediaServerMismatchException(string message) 
            : base(message)
        { }
    }
}