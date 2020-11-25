using System;
using System.Threading;
using System.Threading.Tasks;

namespace FM.LiveSwitch.Hammer
{
    class ClusterTest
    {
        public ClusterTestOptions Options { get; private set; }

        public ClusterTest(ClusterTestOptions options)
        {
            Options = options;
        }

        public async Task<ClusterTestError> Run(CancellationToken cancellationToken)
        {
            try
            {
                for (var i = 0; i < Options.IterationCount; i++)
                {
                    Console.Error.WriteLine($"Test #{i + 1}");

                    var error = await new ClusterTestIteration(Options).Run(cancellationToken).ConfigureAwait(false);
                    if (error != ClusterTestError.None)
                    {
                        if (error == ClusterTestError.Cancelled)
                        {
                            Console.Error.WriteLine("User cancelled.");
                        }
                        return error;
                    }
                }
                return ClusterTestError.None;
            }
            catch (TaskCanceledException)
            {
                Console.Error.WriteLine("User cancelled.");
                return ClusterTestError.Cancelled;
            }
        }
    }
}
