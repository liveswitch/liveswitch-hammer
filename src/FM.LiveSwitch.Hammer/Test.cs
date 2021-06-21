namespace FM.LiveSwitch.Hammer
{
    abstract class Test
    {
        protected Test(Options options)
        {
            if (options.SdkLogLevel != LogLevel.None)
            {
                Log.Provider = new SdkLogProvider(options.SdkLogLevel);
            }
        }
    }
}
