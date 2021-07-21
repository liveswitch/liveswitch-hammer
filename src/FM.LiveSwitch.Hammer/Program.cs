using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FM.LiveSwitch.Hammer
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    }
                };
                settings.Converters.Add(new StringEnumConverter());
                return settings;
            };

            using var parser = new Parser((settings) =>
            {
                settings.CaseInsensitiveEnumValues = true;
                settings.HelpWriter = null;
            });

            var result = parser.ParseArguments<
                ClusterTestOptions,
                LoadTestOptions,
                ScanTestOptions
            >(AppendEnvironmentVariables(args));

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
                Console.Error.WriteLine("Exiting...");
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
        }

        private static void LogException(Exception ex)
        {
            Console.Error.WriteLine($"{ex.Message} {ex.InnerException?.Message}".Trim());
        }

        private static string[] AppendEnvironmentVariables(string[] args)
        {
            if (!TryGetOptions(args.FirstOrDefault(), out var environmentVariablePrefix, out var options))
            {
                return args;
            }

            var newArgs = new List<string>(args);
            foreach (var unusedOption in FilterOptions(args, options))
            {
                var value = Environment.GetEnvironmentVariable($"{environmentVariablePrefix}_{unusedOption.LongName.ToUpperInvariant().Replace("-", "_")}");
                if (value != null)
                {
                    Console.Error.WriteLine($"Environment variable discovered matching --{unusedOption.LongName} option.");
                    newArgs.Add($"--{unusedOption.LongName}={value}");
                }
            }
            return newArgs.ToArray();
        }

        private static bool TryGetOptions(string verb, out string environmentVariablePrefix, out OptionAttribute[] options)
        {
            if (verb != null && verb.StartsWith("-"))
            {
                return TryGetOptions(null, out environmentVariablePrefix, out options);
            }

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract))
            {
                var verbAttribute = type.GetCustomAttributes<VerbAttribute>().FirstOrDefault();
                if (verbAttribute != null)
                {
                    if (verb == null || verbAttribute.Name == verb)
                    {
                        environmentVariablePrefix = Assembly.GetExecutingAssembly().GetName().Name.ToUpperInvariant();
                        if (verb != null)
                        {
                            environmentVariablePrefix = $"{environmentVariablePrefix}_{verb.ToUpperInvariant()}";
                        }

                        options = type.GetProperties()
                            .Select(property => property.GetCustomAttributes<OptionAttribute>().FirstOrDefault())
                            .Where(option => option != null).ToArray();
                        return true;
                    }
                }
            }

            environmentVariablePrefix = null;
            options = null;
            return false;
        }

        private static OptionAttribute[] FilterOptions(string[] args, OptionAttribute[] options)
        {
            var usedLongNames = new HashSet<string>();
            var usedShortNames = new HashSet<string>();

            foreach (var arg in args)
            {
                if (arg.StartsWith("--"))
                {
                    var longName = arg.Substring(2);
                    if (longName.Contains('='))
                    {
                        longName = longName.Substring(0, longName.IndexOf('='));
                    }
                    usedLongNames.Add(longName);
                }
                else if (arg.StartsWith("-"))
                {
                    var shortName = arg.Substring(1);
                    if (shortName.Contains('='))
                    {
                        shortName = shortName.Substring(0, shortName.IndexOf('='));
                    }
                    usedShortNames.Add(shortName);
                }
            }

            return options.Where(option => !usedLongNames.Contains(option.LongName) && !usedShortNames.Contains(option.ShortName)).ToArray();
        }
    }
}
