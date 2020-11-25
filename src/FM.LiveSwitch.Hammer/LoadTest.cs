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
            for (var i = 0; i < Options.IterationCount; i++)
            {
                Console.Error.WriteLine($"Test #{i + 1}");

                var result = await new LoadTestIteration(Options).Run(cancellationToken).ConfigureAwait(false);
                if (result != LoadTestError.None)
                {
                    return result;
                }
            }
            return LoadTestError.None;
        }
    }
}
