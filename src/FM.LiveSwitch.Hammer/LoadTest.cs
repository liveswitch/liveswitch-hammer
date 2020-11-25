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

        public async Task<LoadTestError> Run(CancellationToken cancellationToken)
        {
            try
            {
                for (var i = 0; i < Options.IterationCount; i++)
                {
                    Console.Error.WriteLine($"Test #{i + 1}");

                    var error = await new LoadTestIteration(Options).Run(cancellationToken).ConfigureAwait(false);
                    if (error != LoadTestError.None)
                    {
                        if (error == LoadTestError.Cancelled)
                        {
                            Console.Error.WriteLine("User cancelled.");
                        }
                        return error;
                    }
                }
                return LoadTestError.None;
            }
            catch (TaskCanceledException)
            {
                Console.Error.WriteLine("User cancelled.");
                return LoadTestError.Cancelled;
            }
        }
    }
}
