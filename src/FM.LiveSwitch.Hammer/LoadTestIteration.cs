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

        public async Task Run(CancellationToken cancellationToken)
        {
            try
            {
                await RegisterClients(cancellationToken).ConfigureAwait(false);
                try
                {
                    await JoinChannels(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        await OpenConnections(cancellationToken).ConfigureAwait(false);
                        await Pause(cancellationToken).ConfigureAwait(false);
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
        }

        #region Register and Unregister Clients

        private async Task RegisterClients(CancellationToken cancellationToken)
        {
            _Clients = Enumerable.Range(0, Options.ClientCount).Select(_ => new Client(Options.GatewayUrl, Options.ApplicationId)).ToArray();

            for (var i = 0; i < _Clients.Count(); i += Options.Parallelism)
            {
                var clients = _Clients.Skip(i).Take(Options.Parallelism);

                Console.Error.WriteLine($"Registering {clients.Count()} clients (group #{1 + i / Options.Parallelism})...");

                var stopwatch = new ManagedStopwatch();
                stopwatch.Start();

                var result = await Task.WhenAny(
                    Task.Delay(Timeout.Infinite, cancellationToken),
                    Task.WhenAll(
                        clients.Select(client =>
                        {
                            return client.Register(Token.GenerateClientRegisterToken(client, new ChannelClaim[0], Options.SharedSecret)).AsTask(TaskCreationOptions.RunContinuationsAsynchronously);
                        })
                    )
                ).ConfigureAwait(false);

                stopwatch.Stop();

                // check cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // check faulted
                if (result.IsFaulted)
                {
                    throw new ClientRegisterException("One or more clients could not be registered.", result.Exception);
                }

                Console.Error.WriteLine($"Registering completed in {stopwatch.ElapsedMilliseconds}ms ({clients.Count() * 1000.0 / stopwatch.ElapsedMilliseconds:n2}/s).");
            }
        }

        private async Task UnregisterClients()
        {
            for (var i = 0; i < _Clients.Count(); i += Options.Parallelism)
            {
                var clients = _Clients.Skip(i).Take(Options.Parallelism);

                Console.Error.WriteLine($"Unregistering {clients.Count()} clients (group #{1 + i / Options.Parallelism})...");

                var stopwatch = new ManagedStopwatch();
                stopwatch.Start();

                await Task.WhenAll(
                    clients.Select(client =>
                    {
                        return client.Unregister().AsTask(TaskCreationOptions.RunContinuationsAsynchronously);
                    })
                ).ConfigureAwait(false);

                stopwatch.Stop();

                Console.Error.WriteLine($"Unregistering completed in {stopwatch.ElapsedMilliseconds}ms ({clients.Count() * 1000.0 / stopwatch.ElapsedMilliseconds:n2}/s).");
            }
        }

        #endregion

        #region Join and Leave Channels

        private async Task JoinChannels(CancellationToken cancellationToken)
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

            for (var i = 0; i < _ClientChannels.Count(); i += Options.Parallelism)
            {
                var clientChannels = _ClientChannels.Skip(i).Take(Options.Parallelism);

                Console.Error.WriteLine($"Joining {clientChannels.Count()} channels (group #{1 + i / Options.Parallelism})...");

                var stopwatch = new ManagedStopwatch();
                stopwatch.Start();

                var result = await Task.WhenAny(
                    Task.Delay(Timeout.Infinite, cancellationToken),
                    Task.WhenAll(
                        clientChannels.Select(async cc =>
                        {
                            cc.Channel = await cc.Client.Join(Token.GenerateClientJoinToken(cc.Client, new ChannelClaim(cc.ChannelId), Options.SharedSecret)).AsTask(TaskCreationOptions.RunContinuationsAsynchronously);
                        })
                    )
                ).ConfigureAwait(false);

                stopwatch.Stop();

                // check cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // check faulted
                if (result.IsFaulted)
                {
                    throw new ChannelJoinException("One or more channels could not be joined.", result.Exception);
                }

                Console.Error.WriteLine($"Joining completed in {stopwatch.ElapsedMilliseconds}ms ({clientChannels.Count() * 1000.0 / stopwatch.ElapsedMilliseconds:n2}/s).");
            }
        }

        private async Task LeaveChannels()
        {
            for (var i = 0; i < _ClientChannels.Count(); i += Options.Parallelism)
            {
                var clientChannels = _ClientChannels.Skip(i).Take(Options.Parallelism);

                Console.Error.WriteLine($"Leaving {clientChannels.Count()} channels (group #{1 + i / Options.Parallelism})...");

                var stopwatch = new ManagedStopwatch();
                stopwatch.Start();

                await Task.WhenAll(
                    clientChannels.Select(cc =>
                    {
                        return cc.Client.Leave(cc.ChannelId).AsTask(TaskCreationOptions.RunContinuationsAsynchronously);
                    })
                ).ConfigureAwait(false);

                stopwatch.Stop();

                Console.Error.WriteLine($"Leaving completed in {stopwatch.ElapsedMilliseconds}ms ({clientChannels.Count() * 1000.0 / stopwatch.ElapsedMilliseconds:n2}/s).");
            }
        }

        #endregion

        #region Open and Close Connections

        private async Task OpenConnections(CancellationToken cancellationToken)
        {
            _ClientChannelConnections = _ClientChannels.SelectMany(cc => Enumerable.Range(0, Options.ConnectionCount).Select(_ => new ClientChannelConnection
            {
                Client = cc.Client,
                ChannelId = cc.ChannelId,
                Channel = cc.Channel
            })).ToArray();

            for (var i = 0; i < _ClientChannelConnections.Count(); i += Options.Parallelism)
            {
                var clientChannelConnections = _ClientChannelConnections.Skip(i).Take(Options.Parallelism);

                Console.Error.WriteLine($"Opening {clientChannelConnections.Count()} connections (group #{1 + i / Options.Parallelism})...");

                var stopwatch = new ManagedStopwatch();
                stopwatch.Start();

                var result = await Task.WhenAny(
                    Task.Delay(Timeout.Infinite, cancellationToken),
                    Task.WhenAll(
                        clientChannelConnections.Select(ccc =>
                        {
                            ccc.LocalAudioTrack = new AudioTrack(new NullAudioSource(new Opus.Format
                            {
                                IsPacketized = true
                            }));
                            ccc.LocalVideoTrack = new VideoTrack(new NullVideoSource(new Vp8.Format
                            {
                                IsPacketized = true
                            }));
                            ccc.RemoteAudioTrack = new AudioTrack(new NullAudioSink(new Opus.Format
                            {
                                IsPacketized = true
                            }));
                            ccc.RemoteVideoTrack = new VideoTrack(new NullVideoSink(new Vp8.Format
                            {
                                IsPacketized = true
                            }));

                            ccc.Connection = ccc.Channel.CreateMcuConnection(
                                new AudioStream(ccc.LocalAudioTrack, ccc.RemoteAudioTrack),
                                new VideoStream(ccc.LocalVideoTrack, ccc.RemoteVideoTrack)
                            );

                            return ccc.Connection.Open().AsTaskAsync();
                        })
                    )
                ).ConfigureAwait(false);

                stopwatch.Stop();

                // check cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // check faulted
                if (result.IsFaulted)
                {
                    throw new ConnectionOpenException("One or more connections could not be opened.", result.Exception);
                }

                Console.Error.WriteLine($"Opening completed in {stopwatch.ElapsedMilliseconds}ms ({clientChannelConnections.Count() * 1000.0 / stopwatch.ElapsedMilliseconds:n2}/s).");
            }
        }

        private async Task CloseConnections()
        {
            for (var i = 0; i < _ClientChannelConnections.Count(); i += Options.Parallelism)
            {
                var clientChannelConnections = _ClientChannelConnections.Skip(i).Take(Options.Parallelism);

                Console.Error.WriteLine($"Closing {clientChannelConnections.Count()} connections (group #{1 + i / Options.Parallelism})...");

                var stopwatch = new ManagedStopwatch();
                stopwatch.Start();

                await Task.WhenAll(
                    clientChannelConnections.Select(async ccc =>
                    {
                        await ccc.Connection?.Close();

                        ccc.LocalAudioTrack?.Destroy();
                        ccc.LocalVideoTrack?.Destroy();
                        ccc.RemoteAudioTrack?.Destroy();
                        ccc.RemoteVideoTrack?.Destroy();
                    })
                ).ConfigureAwait(false);

                stopwatch.Stop();

                Console.Error.WriteLine($"Closing completed in {stopwatch.ElapsedMilliseconds}ms ({clientChannelConnections.Count() * 1000.0 / stopwatch.ElapsedMilliseconds:n2}/s).");
            }
        }

        private async Task Pause(CancellationToken cancellationToken)
        {
            Console.Error.WriteLine($"Pausing for {Options.PauseTimeout} seconds...");

            await Task.Delay(Options.PauseTimeout * 1000, cancellationToken).ConfigureAwait(false);

            // check cancellation
            cancellationToken.ThrowIfCancellationRequested();
        }

        #endregion
    }
}
