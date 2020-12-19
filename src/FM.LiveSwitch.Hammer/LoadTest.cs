using System;
using System.Threading;
using System.Threading.Tasks;

namespace FM.LiveSwitch.Hammer
{
    class LoadTest
    {
        public LoadTestOptions Options { get; private set; }

        public LoadTest(LoadTestOptions options)
        {
            Options = options;
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            HttpTransferFactory.CreateHttpTransfer = () => new HttpClientTransfer();
            try
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine($"Warming up...");
                await Warmup(cancellationToken).ConfigureAwait(false);

                for (var i = 0; i < Options.IterationCount; i++)
                {
                    Console.Error.WriteLine();
                    Console.Error.WriteLine($"Test #{i + 1}");
                    await new LoadTestIteration(Options).Run(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                HttpTransferFactory.CreateHttpTransfer = null;
            }
        }

        private Task Warmup(CancellationToken cancellationToken)
        {
            return new LoadTestIteration(new LoadTestOptions
            {
                GatewayUrl = Options.GatewayUrl,
                ApplicationId = Options.ApplicationId,
                SharedSecret = Options.SharedSecret,
                Parallelism = 1,
                ClientCount = 1,
                ChannelCount = 1,
                ConnectionCount = 1,
                IterationCount = 1,
                PauseTimeout = 0
            }).Run(cancellationToken);
        }
    }
}
