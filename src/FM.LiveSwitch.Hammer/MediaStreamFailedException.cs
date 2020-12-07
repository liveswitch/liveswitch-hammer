using System;

namespace FM.LiveSwitch.Hammer
{
    [Serializable]
    public class MediaStreamFailedException : Exception
    {
        public StreamType Type { get; set; }

        public MediaStreamFailedException(StreamType type, string message) 
            : base(message)
        {
            Type = type;
        }
    }
}