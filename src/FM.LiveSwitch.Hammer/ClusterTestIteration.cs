using FM.LiveSwitch;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FM.LiveSwitch.Hammer
{
    class ClusterTestIteration
    {
        public ClusterTestOptions Options { get; private set; }

        public ClusterTestIteration(ClusterTestOptions options)
        {
            Options = options;
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            try
            {
                await RegisterClients(cancellationToken).ConfigureAwait(false);
                try
                {
                    await JoinChannel(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        await StartTracks(cancellationToken).ConfigureAwait(false);
                        try
                        {
                            await OpenConnections(cancellationToken).ConfigureAwait(false);
                            await VerifyConnections(cancellationToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            await CloseConnections().ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        await StopTracks().ConfigureAwait(false);
                    }
                }
                finally
                {
                    await LeaveChannel().ConfigureAwait(false);
                }
            }
            finally
            {
                await UnregisterClients().ConfigureAwait(false);
            }
        }

        #region Register and Unregister Clients

        private Client _Client1;
        private Client _Client2;

        private async Task RegisterClients(CancellationToken cancellationToken)
        {
            Console.Error.WriteLine("Registering clients...");

            _Client1 = new Client(Options.GatewayUrl, Options.ApplicationId, null, null, null, null, Options.Region1)
            {
                Tag = Options.Tag1
            };
            _Client2 = new Client(Options.GatewayUrl, Options.ApplicationId, null, null, null, null, Options.Region2)
            {
                Tag = Options.Tag2
            };

            var result = await Task.WhenAny(
                Task.Delay(Timeout.Infinite, cancellationToken),
                Task.WhenAll(
                    _Client1.Register(Token.GenerateClientRegisterToken(_Client1, new ChannelClaim[0], Options.SharedSecret)).AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                    _Client2.Register(Token.GenerateClientRegisterToken(_Client2, new ChannelClaim[0], Options.SharedSecret)).AsTask(TaskCreationOptions.RunContinuationsAsynchronously)
                )
            ).ConfigureAwait(false);

            // check cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // check faulted
            if (result.IsFaulted)
            {
                throw new ClientRegisterException("One or more clients could not be registered.", result.Exception);
            }
        }

        private Task UnregisterClients()
        {
            Console.Error.WriteLine("Unregistering clients...");

            return Task.WhenAll(
                _Client1.Unregister().AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                _Client2.Unregister().AsTask(TaskCreationOptions.RunContinuationsAsynchronously)
            );
        }

        #endregion

        #region Join and Leave Channels

        private string _ChannelId;

        private Channel _Channel1;
        private Channel _Channel2;

        private async Task JoinChannel(CancellationToken cancellationToken)
        {
            Console.Error.WriteLine("Joining channel...");

            _ChannelId = Utility.GenerateId();

            var channelTasks = Task.WhenAll(
                _Client1.Join(Token.GenerateClientJoinToken(_Client1, _ChannelId, Options.SharedSecret)).AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                _Client2.Join(Token.GenerateClientJoinToken(_Client2, _ChannelId, Options.SharedSecret)).AsTask(TaskCreationOptions.RunContinuationsAsynchronously)
            );
            var result = await Task.WhenAny(
                Task.Delay(Timeout.Infinite, cancellationToken),
                channelTasks
            ).ConfigureAwait(false);

            // check cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // check faulted
            if (result.IsFaulted)
            {
                throw new ChannelJoinException("One or more channels could not be joined.", result.Exception);
            }

            _Channel1 = channelTasks.Result[0];
            _Channel2 = channelTasks.Result[1];
        }

        private Task LeaveChannel()
        {
            Console.Error.WriteLine("Leaving channel...");

            return Task.WhenAll(
                _Client1.Leave(_ChannelId).AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                _Client2.Leave(_ChannelId).AsTask(TaskCreationOptions.RunContinuationsAsynchronously)
            );
        }

        #endregion

        #region Start and Stop Tracks

        private TaskCompletionSource<bool> _SoundDetected1;
        private TaskCompletionSource<bool> _SoundDetected2;
        private TaskCompletionSource<bool> _ImageDetected1;
        private TaskCompletionSource<bool> _ImageDetected2;

        private AudioTrack _LocalAudioTrack1;
        private AudioTrack _LocalAudioTrack2;
        private VideoTrack _LocalVideoTrack1;
        private VideoTrack _LocalVideoTrack2;
        private AudioTrack _RemoteAudioTrack1;
        private AudioTrack _RemoteAudioTrack2;
        private VideoTrack _RemoteVideoTrack1;
        private VideoTrack _RemoteVideoTrack2;

        private async Task StartTracks(CancellationToken cancellationToken)
        {
            Console.Error.WriteLine("Starting tracks...");

            _SoundDetected1 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _SoundDetected2 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ImageDetected1 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ImageDetected2 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _LocalAudioTrack1 = CreateLocalAudioTrack();
            _LocalAudioTrack2 = CreateLocalAudioTrack();
            _LocalVideoTrack1 = CreateLocalVideoTrack();
            _LocalVideoTrack2 = CreateLocalVideoTrack();
            _RemoteAudioTrack1 = CreateRemoteAudioTrack(_SoundDetected1);
            _RemoteAudioTrack2 = CreateRemoteAudioTrack(_SoundDetected2);
            _RemoteVideoTrack1 = CreateRemoteVideoTrack(_ImageDetected1);
            _RemoteVideoTrack2 = CreateRemoteVideoTrack(_ImageDetected2);

            var result = await Task.WhenAny(
                Task.Delay(Timeout.Infinite, cancellationToken),
                Task.WhenAll(
                    _LocalAudioTrack1.Source.Start().AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                    _LocalAudioTrack2.Source.Start().AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                    _LocalVideoTrack1.Source.Start().AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                    _LocalVideoTrack2.Source.Start().AsTask(TaskCreationOptions.RunContinuationsAsynchronously)
                )
            ).ConfigureAwait(false);

            // check cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // check faulted
            if (result.IsFaulted)
            {
                throw new TrackStartException("One or more tracks could not be started.", result.Exception);
            }
        }

        private async Task StopTracks()
        {
            Console.Error.WriteLine("Stopping tracks...");

            await Task.WhenAll(
                _LocalAudioTrack1.Source.Stop().AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                _LocalAudioTrack2.Source.Stop().AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                _LocalVideoTrack1.Source.Stop().AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                _LocalVideoTrack2.Source.Stop().AsTask(TaskCreationOptions.RunContinuationsAsynchronously)
            ).ConfigureAwait(false);

            _LocalAudioTrack1.Destroy();
            _LocalAudioTrack2.Destroy();
            _LocalVideoTrack1.Destroy();
            _LocalVideoTrack2.Destroy();
            _RemoteAudioTrack1.Destroy();
            _RemoteAudioTrack2.Destroy();
            _RemoteVideoTrack1.Destroy();
            _RemoteVideoTrack2.Destroy();
        }

        #endregion

        #region Open and Close Connections

        private int _AudioSendCount1;
        private int _AudioSendCount2;
        private int _AudioReceiveCount1;
        private int _AudioReceiveCount2;
        private int _VideoSendCount1;
        private int _VideoSendCount2;
        private int _VideoReceiveCount1;
        private int _VideoReceiveCount2;

        private McuConnection _Connection1;
        private McuConnection _Connection2;

        private async Task OpenConnections(CancellationToken cancellationToken)
        {
            Console.Error.WriteLine("Opening connections...");

            var audioStream1 = new AudioStream(_LocalAudioTrack1, _RemoteAudioTrack1);
            var audioStream2 = new AudioStream(_LocalAudioTrack2, _RemoteAudioTrack2);
            var videoStream1 = new VideoStream(_LocalVideoTrack1, _RemoteVideoTrack1);
            var videoStream2 = new VideoStream(_LocalVideoTrack2, _RemoteVideoTrack2);

            audioStream1.OnProcessFrame += (f) => _AudioSendCount1++;
            audioStream2.OnProcessFrame += (f) => _AudioSendCount2++;
            audioStream1.OnRaiseFrame += (f) => _AudioReceiveCount1++;
            audioStream2.OnRaiseFrame += (f) => _AudioReceiveCount2++;
            videoStream1.OnProcessFrame += (f) => _VideoSendCount1++;
            videoStream2.OnProcessFrame += (f) => _VideoSendCount2++;
            videoStream1.OnRaiseFrame += (f) => _VideoReceiveCount1++;
            videoStream2.OnRaiseFrame += (f) => _VideoReceiveCount2++;

            _Connection1 = _Channel1.CreateMcuConnection(audioStream1, videoStream1);
            _Connection2 = _Channel2.CreateMcuConnection(audioStream2, videoStream2);

            var result = await Task.WhenAny(
                Task.Delay(Timeout.Infinite, cancellationToken),
                Task.WhenAll(
                    _Connection1.Open().AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                    _Connection2.Open().AsTask(TaskCreationOptions.RunContinuationsAsynchronously)
                )
            ).ConfigureAwait(false);

            // check cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // check faulted
            if (result.IsFaulted)
            {
                throw new ConnectionOpenException("One or more connections could not be opened.", result.Exception);
            }
        }

        private Task CloseConnections()
        {
            Console.Error.WriteLine("Closing connections...");

            return Task.WhenAll(
                _Connection1.Close().AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                _Connection2.Close().AsTask(TaskCreationOptions.RunContinuationsAsynchronously)
            );
        }

        private async Task VerifyConnections(CancellationToken cancellationToken)
        {
            Console.Error.WriteLine("Verifying media...");

            await Task.WhenAny(
                Task.Delay(Options.MediaTimeout * 1000, cancellationToken),
                Task.WhenAll(
                    _SoundDetected1.Task,
                    _SoundDetected2.Task,
                    _ImageDetected1.Task,
                    _ImageDetected2.Task
                )
            ).ConfigureAwait(false);

            // check cancellation
            cancellationToken.ThrowIfCancellationRequested();

            if (!_SoundDetected1.Task.IsCompleted || _SoundDetected1.Task.IsFaulted)
            {
                throw new MediaStreamFailedException(StreamType.Audio, $"Audio stream #1 failed. Sent {_AudioSendCount1} frames, but received {_AudioReceiveCount1} frames.");
            }

            if (!_SoundDetected2.Task.IsCompleted || _SoundDetected2.Task.IsFaulted)
            {
                throw new MediaStreamFailedException(StreamType.Audio, $"Audio stream #2 failed. Sent {_AudioSendCount2} frames, but received {_AudioReceiveCount2} frames.");
            }

            if (!_ImageDetected1.Task.IsCompleted || _ImageDetected1.Task.IsFaulted)
            {
                throw new MediaStreamFailedException(StreamType.Video, $"Video stream #1 failed. Sent {_VideoSendCount1} frames, but received {_VideoReceiveCount1} frames.");
            }

            if (!_ImageDetected2.Task.IsCompleted || _ImageDetected2.Task.IsFaulted)
            {
                throw new MediaStreamFailedException(StreamType.Video, $"Video stream #2 failed. Sent {_VideoSendCount2} frames, but received {_VideoReceiveCount2} frames.");
            }
        }

        #endregion

        #region Create Tracks

        private AudioTrack CreateLocalAudioTrack()
        {
            return new AudioTrack(new FakeAudioSource(Opus.Format.DefaultConfig))
                .Next(new Opus.Encoder())
                .Next(new Opus.Packetizer());
        }

        private AudioTrack CreateRemoteAudioTrack(TaskCompletionSource<bool> soundDetected)
        {
            return new AudioTrack(new Opus.Depacketizer())
                .Next(new Opus.Decoder())
                .Next(new SoundDetectionSink
            {
                SoundDetected = soundDetected
            });
        }

        private VideoTrack CreateLocalVideoTrack()
        {
            return new VideoTrack(new FakeVideoSource(new VideoConfig(320, 240, 30), VideoFormat.I420))
                .Next(new Vp8.Encoder())
                .Next(new Vp8.Packetizer());
        }

        private VideoTrack CreateRemoteVideoTrack(TaskCompletionSource<bool> imageDetected)
        {
            return new VideoTrack(new Vp8.Depacketizer())
                .Next(new Vp8.Decoder())
                .Next(new ImageDetectionSink
                {
                    ImageDetected = imageDetected
                });
        }

        #endregion
    }
}
