using System;
using System.Threading;
using System.Threading.Tasks;

namespace FM.LiveSwitch.Hammer
{
    class ClusterTest : Test
    {
        public ClusterTestOptions Options { get; private set; }

        public ClusterTest(ClusterTestOptions options)
            : base(options)
        {
            Options = options;
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            for (var i = 0; i < Options.IterationCount; i++)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine($"Test #{i + 1}");
                await new ClusterTestIteration(Options).Run(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
