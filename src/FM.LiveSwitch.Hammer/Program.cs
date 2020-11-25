using CommandLine;
using CommandLine.Text;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FM.LiveSwitch.Hammer
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            using var parser = new Parser((settings) =>
            {
                settings.CaseInsensitiveEnumValues = true;
                settings.HelpWriter = null;
            });

            var result = parser.ParseArguments<
                ClusterTestOptions,
                LoadTestOptions
            >(args);

            var cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(() =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        cancellationTokenSource.Cancel();
                    }
                }
            });

            result.MapResult(
                (ClusterTestOptions options) =>
                {
                    return Task.Run(async () =>
                    {
                        return (int)await new ClusterTest(options).Run(cancellationTokenSource.Token);
                    }).GetAwaiter().GetResult();
                },
                (LoadTestOptions options) =>
                {
                    return Task.Run(async () =>
                    {
                        return (int)await new LoadTest(options).Run(cancellationTokenSource.Token);
                    }).GetAwaiter().GetResult();
                },
                errors =>
                {
                    var helpText = HelpText.AutoBuild(result);
                    helpText.Copyright = "Copyright (C) 2020 Frozen Mountain Software Ltd.";
                    helpText.AddEnumValuesToHelpText = true;
                    Console.Error.Write(helpText);
                    return 1;
                });
        }
    }
}
