namespace FM.LiveSwitch.Hammer
{
    enum ClusterTestError
    {
        None,
        Cancelled,
        ClientRegisterFailed,
        ChannelJoinFailed,
        TrackStartFailed,
        ConnectionOpenFailed,
        AudioStream1Failed,
        AudioStream2Failed,
        VideoStream1Failed,
        VideoStream2Failed
    }
}
