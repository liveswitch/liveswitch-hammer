using FM.LiveSwitch;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FM.LiveSwitch.Hammer
{
    class LoadTestIteration
    {
        public LoadTestOptions Options { get; private set; }

        public LoadTestIteration(LoadTestOptions options)
        {
            Options = options;
        }

        class ClientChannel
        {
            public Client Client { get; set; }
            public string ChannelId { get; set; }
            public Channel Channel { get; set; }
        }

        class ClientChannelConnection
        {
            public Client Client { get; set; }
            public string ChannelId { get; set; }
            public Channel Channel { get; set; }
            public AudioTrack LocalAudioTrack { get; set; }
            public VideoTrack LocalVideoTrack { get; set; }
            public AudioTrack RemoteAudioTrack { get; set; }
            public VideoTrack RemoteVideoTrack { get; set; }
            public McuConnection Connection { get; set; }
        }

        private Client[] _Clients;
        private ClientChannel[] _ClientChannels;
        private ClientChannelConnection[] _ClientChannelConnections;

        public async Task<LoadTestError> Run(CancellationToken cancellationToken)
        {
            LoadTestError result;
            try
            {
                result = await RegisterClients(cancellationToken).ConfigureAwait(false);
                if (result != LoadTestError.None)
                {
                    return result;
                }

                try
                {
                    result = await JoinChannels(cancellationToken).ConfigureAwait(false);
                    if (result != LoadTestError.None)
                    {
                        return result;
                    }

                    try
                    {
                        result = await OpenConnections(cancellationToken).ConfigureAwait(false);
                        if (result != LoadTestError.None)
                        {
                            return result;
                        }
                    }
                    finally
                    {
                        await CloseConnections().ConfigureAwait(false);
                    }
                }
                finally
                {
                    await LeaveChannels().ConfigureAwait(false);
                }
            }
            finally
            {
                await UnregisterClients().ConfigureAwait(false);
            }
            return result;
        }

        private async Task<LoadTestError> RegisterClients(CancellationToken cancellationToken)
        {
            _Clients = Enumerable.Range(0, Options.ClientCount).Select(_ => new Client(Options.GatewayUrl, Options.ApplicationId)).ToArray();

            for (var i = 0; i < _Clients.Count(); i += Options.ParallelClientRegisters)
            {
                var clients = _Clients.Skip(i).Take(Options.ParallelClientRegisters);

                Console.Error.WriteLine($" Registering clients (group #{1 + i / Options.ParallelClientRegisters}, {clients.Count()} register operations)...");

                var result = await Task.WhenAny(
                    Task.Delay(Timeout.Infinite, cancellationToken),
                    Task.WhenAll(
                        clients.Select(client =>
                        {
                            return client.Register(Token.GenerateClientRegisterToken(client, new ChannelClaim[0], Options.SharedSecret)).AsTask(TaskCreationOptions.RunContinuationsAsynchronously);
                        })
                    )
                ).ConfigureAwait(false);

                // check cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    return LoadTestError.Cancelled;
                }

                // check faulted
                if (result.IsFaulted)
                {
                    return LoadTestError.ClientRegisterFailed;
                }
            }
            return LoadTestError.None;
        }

        private async Task UnregisterClients()
        {
            for (var i = 0; i < _Clients.Count(); i += Options.ParallelClientRegisters)
            {
                var clients = _Clients.Skip(i).Take(Options.ParallelClientRegisters);

                Console.Error.WriteLine($" Unregistering clients (group #{1 + i / Options.ParallelClientRegisters}, {clients.Count()} unregister operations)...");

                await Task.WhenAll(
                    clients.Select(client =>
                    {
                        return client.Unregister().AsTask(TaskCreationOptions.RunContinuationsAsynchronously);
                    })
                ).ConfigureAwait(false);
            }
        }

        private async Task<LoadTestError> JoinChannels(CancellationToken cancellationToken)
        {
            var channelIds = Enumerable.Range(0, Options.ChannelCount).Select(_ => Utility.GenerateId()).ToArray();

            if (Options.ChannelBurst)
            {
                _ClientChannels = channelIds.SelectMany(channelId => _Clients.Select(client => new ClientChannel
                {
                    Client = client,
                    ChannelId = channelId
                })).ToArray();
            }
            else
            {
                _ClientChannels = _Clients.SelectMany(client => channelIds.Select(channelId => new ClientChannel
                {
                    Client = client,
                    ChannelId = channelId
                })).ToArray();
            }

            for (var i = 0; i < _ClientChannels.Count(); i += Options.ParallelChannelJoins)
            {
                var clientChannels = _ClientChannels.Skip(i).Take(Options.ParallelChannelJoins);

                Console.Error.WriteLine($" Joining channels (group #{1 + i / Options.ParallelChannelJoins}, {clientChannels.Count()} join operations)...");

                var result = await Task.WhenAny(
                    Task.Delay(Timeout.Infinite, cancellationToken),
                    Task.WhenAll(
                        clientChannels.Select(async cc =>
                        {
                            cc.Channel = await cc.Client.Join(Token.GenerateClientJoinToken(cc.Client, new ChannelClaim(cc.ChannelId), Options.SharedSecret)).AsTask(TaskCreationOptions.RunContinuationsAsynchronously);
                        })
                    )
                ).ConfigureAwait(false);

                // check cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    return LoadTestError.Cancelled;
                }

                // check faulted
                if (result.IsFaulted)
                {
                    return LoadTestError.ChannelJoinFailed;
                }
            }
            return LoadTestError.None;
        }

        private async Task LeaveChannels()
        {
            for (var i = 0; i < _ClientChannels.Count(); i += Options.ParallelChannelJoins)
            {
                var clientChannels = _ClientChannels.Skip(i).Take(Options.ParallelChannelJoins);

                Console.Error.WriteLine($" Leaving channels (group #{1 + i / Options.ParallelChannelJoins}, {clientChannels.Count()} leave operations)...");

                await Task.WhenAll(
                    clientChannels.Select(cc =>
                    {
                        return cc.Client.Leave(cc.ChannelId).AsTask(TaskCreationOptions.RunContinuationsAsynchronously);
                    })
                ).ConfigureAwait(false);
            }
        }

        private async Task<LoadTestError> OpenConnections(CancellationToken cancellationToken)
        {
            _ClientChannelConnections = _ClientChannels.SelectMany(cc => Enumerable.Range(0, Options.ConnectionCount).Select(_ => new ClientChannelConnection
            {
                Client = cc.Client,
                ChannelId = cc.ChannelId,
                Channel = cc.Channel
            })).ToArray();

            for (var i = 0; i < _ClientChannelConnections.Count(); i += Options.ParallelConnectionOpens)
            {
                var clientChannelConnections = _ClientChannelConnections.Skip(i).Take(Options.ParallelConnectionOpens);

                Console.Error.WriteLine($" Opening connections (group #{1 + i / Options.ParallelConnectionOpens}, {clientChannelConnections.Count()} open operations)...");

                var result = await Task.WhenAny(
                    Task.Delay(Timeout.Infinite, cancellationToken),
                    Task.WhenAll(
                        clientChannelConnections.Select(async ccc =>
                        {
                            ccc.LocalAudioTrack = CreateLocalAudioTrack();
                            ccc.LocalVideoTrack = CreateLocalVideoTrack();
                            ccc.RemoteAudioTrack = CreateRemoteAudioTrack();
                            ccc.RemoteVideoTrack = CreateRemoteVideoTrack();

                            ccc.Connection = ccc.Channel.CreateMcuConnection(
                                new AudioStream(ccc.LocalAudioTrack, ccc.RemoteAudioTrack),
                                new VideoStream(ccc.LocalVideoTrack, ccc.RemoteVideoTrack)
                            );

                            await ccc.Connection.Open();
                        })
                    )
                ).ConfigureAwait(false);

                // check cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    return LoadTestError.Cancelled;
                }

                // check faulted
                if (result.IsFaulted)
                {
                    return LoadTestError.ConnectionOpenFailed;
                }
            }
            return LoadTestError.None;
        }

        private async Task CloseConnections()
        {
            for (var i = 0; i < _ClientChannelConnections.Count(); i += Options.ParallelConnectionOpens)
            {
                var clientChannelConnections = _ClientChannelConnections.Skip(i).Take(Options.ParallelConnectionOpens);

                Console.Error.WriteLine($" Closing connections (group #{1 + i / Options.ParallelConnectionOpens}, {clientChannelConnections.Count()} close operations)...");

                await Task.WhenAll(
                    clientChannelConnections.Select(async ccc =>
                    {
                        await ccc.Connection.Close();

                        ccc.LocalAudioTrack.Destroy();
                        ccc.LocalVideoTrack.Destroy();
                        ccc.RemoteAudioTrack.Destroy();
                        ccc.RemoteVideoTrack.Destroy();
                    })
                ).ConfigureAwait(false);
            }
        }

        private AudioTrack CreateLocalAudioTrack()
        {
            return new AudioTrack(new NullAudioSource(new Opus.Format
            {
                IsPacketized = true
            }));
        }

        private AudioTrack CreateRemoteAudioTrack()
        {
            return new AudioTrack(new NullAudioSink(new Opus.Format
            {
                IsPacketized = true
            }));
        }

        private VideoTrack CreateLocalVideoTrack()
        {
            return new VideoTrack(new NullVideoSource(new Vp8.Format
            {
                IsPacketized = true
            }));
        }

        private VideoTrack CreateRemoteVideoTrack()
        {
            return new VideoTrack(new NullVideoSink(new Vp8.Format
            {
                IsPacketized = true
            }));
        }
    }
}
