using FM.LiveSwitch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Opus = FM.LiveSwitch.Opus;
using Vp8 = FM.LiveSwitch.Vp8;

namespace FM.LiveSwitch.Hammer
{
    class ClusterTestIteration
    {
        public ClusterTestOptions Options { get; private set; }

        public ClusterTestIteration(ClusterTestOptions options)
        {
            Options = options;
        }

        private string _ChannelId;

        private Client _Client1;
        private Client _Client2;

        private Channel _Channel1;
        private Channel _Channel2;

        private TaskCompletionSource<bool> _AudioVerify1;
        private TaskCompletionSource<bool> _AudioVerify2;
        private TaskCompletionSource<bool> _VideoVerify1;
        private TaskCompletionSource<bool> _VideoVerify2;

        private AudioTrack _LocalAudioTrack1;
        private AudioTrack _LocalAudioTrack2;
        private VideoTrack _LocalVideoTrack1;
        private VideoTrack _LocalVideoTrack2;
        private AudioTrack _RemoteAudioTrack1;
        private AudioTrack _RemoteAudioTrack2;
        private VideoTrack _RemoteVideoTrack1;
        private VideoTrack _RemoteVideoTrack2;

        private AudioStream _AudioStream1;
        private AudioStream _AudioStream2;
        private VideoStream _VideoStream1;
        private VideoStream _VideoStream2;

        private McuConnection _Connection1;
        private McuConnection _Connection2;

        private int _AudioSendCount1;
        private int _AudioSendCount2;
        private int _AudioReceiveCount1;
        private int _AudioReceiveCount2;
        private int _VideoSendCount1;
        private int _VideoSendCount2;
        private int _VideoReceiveCount1;
        private int _VideoReceiveCount2;

        public async Task<ClusterTestError> Run(CancellationToken cancellationToken)
        {
            var error = ClusterTestError.None;
            try
            {
                error = await RegisterClients(cancellationToken).ConfigureAwait(false);
                if (error != ClusterTestError.None)
                {
                    return error;
                }

                try
                {
                    error = await JoinChannel(cancellationToken).ConfigureAwait(false);
                    if (error != ClusterTestError.None)
                    {
                        return error;
                    }

                    try
                    {
                        error = await StartTracks(cancellationToken).ConfigureAwait(false);
                        if (error != ClusterTestError.None)
                        {
                            return error;
                        }

                        try
                        {
                            error = await OpenConnections(cancellationToken).ConfigureAwait(false);
                            if (error != ClusterTestError.None)
                            {
                                return error;
                            }

                            error = await Verify(cancellationToken).ConfigureAwait(false);
                            if (error != ClusterTestError.None)
                            {
                                return error;
                            }
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
            return error;
        }

        private async Task<ClusterTestError> RegisterClients(CancellationToken cancellationToken)
        {
            Console.Error.WriteLine("  Registering clients...");

            _Client1 = new Client(Options.GatewayUrl, Options.ApplicationId, Options.User1, Options.Device1, null, null, Options.Region1);
            _Client2 = new Client(Options.GatewayUrl, Options.ApplicationId, Options.User2, Options.Device2, null, null, Options.Region2);

            var result = await Task.WhenAny(
                Task.Delay(Timeout.Infinite, cancellationToken),
                Task.WhenAll(
                    _Client1.Register(Token.GenerateClientRegisterToken(_Client1, new ChannelClaim[0], Options.SharedSecret)).AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                    _Client2.Register(Token.GenerateClientRegisterToken(_Client2, new ChannelClaim[0], Options.SharedSecret)).AsTask(TaskCreationOptions.RunContinuationsAsynchronously)
                )
            ).ConfigureAwait(false);

            // check cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                return ClusterTestError.Cancelled;
            }

            // check faulted
            if (result.IsFaulted)
            {
                return ClusterTestError.ClientRegisterFailed;
            }

            return ClusterTestError.None;
        }

        private Task UnregisterClients()
        {
            Console.Error.WriteLine("  Unregistering clients...");

            return Task.WhenAll(
                _Client1.Unregister().AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                _Client2.Unregister().AsTask(TaskCreationOptions.RunContinuationsAsynchronously)
            );
        }

        private async Task<ClusterTestError> JoinChannel(CancellationToken cancellationToken)
        {
            Console.Error.WriteLine("  Joining channel...");

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
            if (cancellationToken.IsCancellationRequested)
            {
                return ClusterTestError.Cancelled;
            }

            // check faulted
            if (result.IsFaulted)
            {
                return ClusterTestError.ChannelJoinFailed;
            }

            _Channel1 = channelTasks.Result[0];
            _Channel2 = channelTasks.Result[1];

            return ClusterTestError.None;
        }

        private Task LeaveChannel()
        {
            Console.Error.WriteLine("  Leaving channel...");

            return Task.WhenAll(
                _Client1.Leave(_ChannelId).AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                _Client2.Leave(_ChannelId).AsTask(TaskCreationOptions.RunContinuationsAsynchronously)
            );
        }

        private async Task<ClusterTestError> StartTracks(CancellationToken cancellationToken)
        {
            Console.Error.WriteLine("  Starting tracks...");

            _AudioVerify1 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _AudioVerify2 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _VideoVerify1 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _VideoVerify2 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _LocalAudioTrack1 = CreateLocalAudioTrack();
            _LocalAudioTrack2 = CreateLocalAudioTrack();
            _LocalVideoTrack1 = CreateLocalVideoTrack();
            _LocalVideoTrack2 = CreateLocalVideoTrack();
            _RemoteAudioTrack1 = CreateRemoteAudioTrack(_AudioVerify1);
            _RemoteAudioTrack2 = CreateRemoteAudioTrack(_AudioVerify2);
            _RemoteVideoTrack1 = CreateRemoteVideoTrack(_VideoVerify1);
            _RemoteVideoTrack2 = CreateRemoteVideoTrack(_VideoVerify2);

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
            if (cancellationToken.IsCancellationRequested)
            {
                return ClusterTestError.Cancelled;
            }

            // check faulted
            if (result.IsFaulted)
            {
                return ClusterTestError.TrackStartFailed;
            }

            return ClusterTestError.None;
        }

        private async Task StopTracks()
        {
            Console.Error.WriteLine("  Stopping tracks...");

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

        private async Task<ClusterTestError> OpenConnections(CancellationToken cancellationToken)
        {
            Console.Error.WriteLine("  Opening connections...");

            _AudioStream1 = new AudioStream(_LocalAudioTrack1, _RemoteAudioTrack1);
            _AudioStream2 = new AudioStream(_LocalAudioTrack2, _RemoteAudioTrack2);
            _VideoStream1 = new VideoStream(_LocalVideoTrack1, _RemoteVideoTrack1);
            _VideoStream2 = new VideoStream(_LocalVideoTrack2, _RemoteVideoTrack2);

            _AudioStream1.OnProcessFrame += (f) => _AudioSendCount1++;
            _AudioStream2.OnProcessFrame += (f) => _AudioSendCount2++;
            _AudioStream1.OnRaiseFrame += (f) => _AudioReceiveCount1++;
            _AudioStream2.OnRaiseFrame += (f) => _AudioReceiveCount2++;
            _VideoStream1.OnProcessFrame += (f) => _VideoSendCount1++;
            _VideoStream2.OnProcessFrame += (f) => _VideoSendCount2++;
            _VideoStream1.OnRaiseFrame += (f) => _VideoReceiveCount1++;
            _VideoStream2.OnRaiseFrame += (f) => _VideoReceiveCount2++;

            _Connection1 = _Channel1.CreateMcuConnection(_AudioStream1, _VideoStream1);
            _Connection2 = _Channel2.CreateMcuConnection(_AudioStream2, _VideoStream2);

            var result = await Task.WhenAny(
                Task.Delay(Timeout.Infinite, cancellationToken),
                Task.WhenAll(
                    _Connection1.Open().AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                    _Connection2.Open().AsTask(TaskCreationOptions.RunContinuationsAsynchronously)
                )
            ).ConfigureAwait(false);

            // check cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                return ClusterTestError.Cancelled;
            }

            // check faulted
            if (result.IsFaulted)
            {
                return ClusterTestError.ConnectionOpenFailed;
            }

            return ClusterTestError.None;
        }

        private Task CloseConnections()
        {
            Console.Error.WriteLine("  Closing connections...");

            return Task.WhenAll(
                _Connection1.Close().AsTask(TaskCreationOptions.RunContinuationsAsynchronously),
                _Connection2.Close().AsTask(TaskCreationOptions.RunContinuationsAsynchronously)
            );
        }

        private async Task<ClusterTestError> Verify(CancellationToken cancellationToken)
        {
            Console.Error.WriteLine("  Verifying media...");

            await Task.WhenAny(
                Task.Delay(Options.MediaTimeout, cancellationToken),
                Task.WhenAll(
                    _AudioVerify1.Task,
                    _AudioVerify2.Task,
                    _VideoVerify1.Task,
                    _VideoVerify2.Task
                )
            ).ConfigureAwait(false);

            // check cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                return ClusterTestError.Cancelled;
            }

            if (!_AudioVerify1.Task.IsCompleted || _AudioVerify1.Task.IsFaulted)
            {
                Console.Error.WriteLine($"  Audio 1 failed (sent: {_AudioSendCount1}, received: {_AudioReceiveCount1}).");
                return ClusterTestError.AudioStream1Failed;
            }

            if (!_AudioVerify2.Task.IsCompleted || _AudioVerify2.Task.IsFaulted)
            {
                Console.Error.WriteLine($"  Audio 2 failed (sent: {_AudioSendCount2}, received: {_AudioReceiveCount2}).");
                return ClusterTestError.AudioStream2Failed;
            }

            if (!_VideoVerify1.Task.IsCompleted || _VideoVerify1.Task.IsFaulted)
            {
                Console.Error.WriteLine($"  Video 1 failed (sent: {_VideoSendCount1}, received: {_VideoReceiveCount1}).");
                return ClusterTestError.VideoStream1Failed;
            }

            if (!_VideoVerify2.Task.IsCompleted || _VideoVerify2.Task.IsFaulted)
            {
                Console.Error.WriteLine($"  Video 2 failed (sent: {_VideoSendCount2}, received: {_VideoReceiveCount2}).");
                return ClusterTestError.VideoStream2Failed;
            }

            return ClusterTestError.None;
        }

        private AudioTrack CreateLocalAudioTrack()
        {
            var source = new FakeAudioSource(Opus.Format.DefaultConfig);
            var encoder = new Opus.Encoder();
            var packetizer = new Opus.Packetizer();
            return new AudioTrack(source).Next(encoder).Next(packetizer);
        }

        private AudioTrack CreateRemoteAudioTrack(TaskCompletionSource<bool> verify)
        {
            var depacketizer = new Opus.Depacketizer();
            var decoder = new Opus.Decoder();
            var sink = new NullAudioSink();
            sink.OnProcessFrame += (frame) =>
            {
                var dataBuffer = frame.LastBuffer.DataBuffer;

                var samples = new List<int>();
                for (var i = 0; i < dataBuffer.Length; i += sizeof(short))
                {
                    samples.Add(dataBuffer.Read16Signed(i));
                }

                var max = samples.Max();
                var min = samples.Min();
                if (min < -1 && max > 1) // not silent
                {
                    verify.TrySetResult(true);
                }
            };
            return new AudioTrack(depacketizer).Next(decoder).Next(sink);
        }

        private VideoTrack CreateLocalVideoTrack()
        {
            var source = new FakeVideoSource(new VideoConfig(320, 240, 30), VideoFormat.I420);
            var encoder = new Vp8.Encoder();
            var packetizer = new Vp8.Packetizer();
            return new VideoTrack(source).Next(encoder).Next(packetizer);
        }

        private VideoTrack CreateRemoteVideoTrack(TaskCompletionSource<bool> verify)
        {
            var depacketizer = new Vp8.Depacketizer();
            var decoder = new Vp8.Decoder();
            var sink = new NullVideoSink();
            sink.OnProcessFrame += (frame) =>
            {
                var buffer = frame.LastBuffer;

                var point = new Point(buffer.Width / 2, buffer.Height / 2);

                int r, g, b;
                if (buffer.IsRgbType)
                {
                    var index = point.Y * buffer.Width + point.X;
                    r = buffer.GetRValue(index);
                    g = buffer.GetGValue(index);
                    b = buffer.GetBValue(index);
                }
                else
                {
                    var yIndex = point.Y * buffer.Width + point.X;
                    var uvIndex = point.Y / 2 * buffer.Width / 2 + point.X / 2;

                    var y = buffer.GetYValue(yIndex);
                    var u = buffer.GetUValue(uvIndex);
                    var v = buffer.GetVValue(uvIndex);

                    // Rec.601
                    var kr = 0.299;
                    var kg = 0.587;
                    var kb = 0.114;

                    r = Math.Max(0, Math.Min(255, (int)(y + 2 * (v - 128) * (1 - kr))));
                    g = Math.Max(0, Math.Min(255, (int)(y - 2 * (u - 128) * (1 - kb) * kb / kg - 2 * (v - 128) * (1 - kr) * kr / kg)));
                    b = Math.Max(0, Math.Min(255, (int)(y + 2 * (u - 128) * (1 - kb))));
                }

                var max = new[] { r, g, b }.Max();
                if (max > 15) // not black
                {
                    verify.TrySetResult(true);
                }
            };
            return new VideoTrack(depacketizer).Next(decoder).Next(sink);
        }
    }
}
