using System;

namespace FM.LiveSwitch.Hammer
{
    class SdkLogProvider : LogProvider
    {
        public SdkLogProvider(LogLevel level)
        {
            Level = level;
        }

        protected override void DoLog(LogEvent logEvent)
        {
            Console.Error.WriteLine(GenerateLogLine(logEvent));
        }
    }
}
