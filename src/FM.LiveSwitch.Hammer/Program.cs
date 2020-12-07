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
                LoadTestOptions,
                ScanTestOptions
            >(args);

            result.MapResult(
                (ClusterTestOptions options) =>
                {
                    return Run(new ClusterTest(options).Run).GetAwaiter().GetResult();
                },
                (LoadTestOptions options) =>
                {
                    return Run(new LoadTest(options).Run).GetAwaiter().GetResult();
                },
                (ScanTestOptions options) =>
                {
                    return Run(new ScanTest(options).Run).GetAwaiter().GetResult();
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

        private static async Task<int> Run(Func<CancellationToken, Task> func)
        {
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

            try
            {
                await func(cancellationTokenSource.Token).ConfigureAwait(false);

                Console.Error.WriteLine();
                Console.Error.WriteLine("Success!");

                return 0;
            }
            catch (TaskCanceledException ex)
            {
                LogException(ex);
                return 1;
            }
            catch (ClientRegisterException ex)
            {
                LogException(ex);
                return 2;
            }
            catch (ChannelJoinException ex)
            {
                LogException(ex);
                return 3;
            }
            catch (TrackStartException ex)
            {
                LogException(ex);
                return 4;
            }
            catch (ConnectionOpenException ex)
            {
                LogException(ex);
                return 5;
            }
            catch (MediaStreamFailedException ex)
            {
                LogException(ex);
                return 6;
            }
            catch (CertificateExpiringException ex)
            {
                LogException(ex);
                return 7;
            }
        }

        private static void LogException(Exception ex)
        {
            Console.Error.WriteLine($"{ex.Message} {ex.InnerException?.Message}".Trim());
        }
    }
}
