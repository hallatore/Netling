using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommandLine.Options;
using Netling.Core;
using Netling.Core.HttpClientWorker;
using Netling.Core.Models;
using Netling.Core.SocketWorker;

namespace Netling.ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en");

            var threads = 1;
            var duration = 10;
            int? count = null;
            int? warmupOpt = null;
            int warmup = 0;
            string testname = string.Empty;

            var p = new OptionSet()
            {
                {"t|threads=", (int v) => threads = v},
                {"d|duration=", (int v) => duration = v},
                {"c|count=", (int? v) => count = v},
                {"w|warmup=", (int? w) => warmupOpt = w},
                {"n|testname=", (string n) => testname = n},
            };

            var extraArgs = p.Parse(args);
            var url = extraArgs.FirstOrDefault(e => e.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || e.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
            Uri uri = null;
            PluginInfo pluginTest = null;

            if (string.IsNullOrEmpty(testname))
            {
                ShowHelp();
                return;
            }
            else
            {
                List<PluginInfo> pluginList = PluginLoader.GetPluginList(".");
                pluginTest = pluginList.Where(x => x.TypeName == testname).FirstOrDefault();
            }

            if (warmupOpt.HasValue)
            {
                warmup = warmupOpt.Value;
            }

            if (url != null && !Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                Console.WriteLine("Failed to parse URL");
            }
            else if (pluginTest != null && url != null && count.HasValue)
            {
                RunWithCount(uri, pluginTest, count.Value, TimeSpan.FromSeconds(warmup)).Wait();
            }
            else if (pluginTest != null && url != null)
            {
                RunWithDuration(uri, pluginTest, threads, TimeSpan.FromSeconds(duration), TimeSpan.FromSeconds(warmup)).Wait();
            }
            else
            {
                ShowHelp();
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine(HelpString);

            List<PluginInfo> pluginList = PluginLoader.GetPluginList(".");

            Console.WriteLine("Plugins Availiable:");
            foreach (var plugin in pluginList)
            {
                Console.WriteLine("{0} {1}", plugin.TypeName, plugin.AssemblyShort);
            }
        }

        private static Task RunWithCount(Uri uri, PluginInfo selectedPlugin, int count, TimeSpan warmupDuration)
        {
            Console.WriteLine(StartRunWithCountString, count, uri);
            return Run(uri, selectedPlugin, 1, TimeSpan.MaxValue, warmupDuration, count);
        }

        private static Task RunWithDuration(Uri uri, PluginInfo selectedPlugin, int threads, TimeSpan duration, TimeSpan warmupDuration)
        {
            Console.WriteLine(StartRunWithDurationString, duration.TotalSeconds, warmupDuration, uri, threads);
            return Run(uri, selectedPlugin, threads, duration, warmupDuration, null);
        }

        private static async Task Run(Uri uri, PluginInfo selectedPlugin, int threads, TimeSpan duration, TimeSpan warmupDuration, int? count)
        {
            WorkerResult result;

            Console.WriteLine("Running tests from {0} : {1}", selectedPlugin.AssemblyShort, selectedPlugin.TypeName);

            Assembly workerPluginAssembly = PluginLoader.LoadPlugin(selectedPlugin.AssemblyName);
            var workerPlugin = PluginLoader.CreateInstance(workerPluginAssembly, selectedPlugin.TypeName);
            workerPlugin.Initialize(uri);

            var worker = new Worker(workerPlugin);

            if (count.HasValue)
            {
                result = await worker.Run(uri.ToString(), count.Value, warmupDuration, new CancellationToken());
            }
            else
            {
                result = await worker.Run(uri.ToString(), threads, duration, warmupDuration, new CancellationToken());
            }

            Console.WriteLine(ResultString1,
                result.Count,
                result.Elapsed.TotalSeconds,
                result.RequestsPerSecond,
                result.Bandwidth,
                result.Errors);

            Console.WriteLine(ResultString2,
                result.Median,
                result.StdDev,
                result.Min,
                result.Max,
                result.P99,
                result.P95,
                result.P50,
                GetAsciiHistogram(result));
        }

        private static string GetAsciiHistogram(WorkerResult workerResult)
        {
            if (workerResult.Histogram.Length == 0)
            {
                return string.Empty;
            }

            const string filled = "█";
            const string empty = " ";
            var histogramText = new string[7];
            var max = workerResult.Histogram.Max();

            foreach (var t in workerResult.Histogram)
            {
                for (var j = 0; j < histogramText.Length; j++)
                {
                    histogramText[j] += t > max / histogramText.Length * (histogramText.Length - j - 1) ? filled : empty;
                }
            }

            var text = string.Join("\r\n", histogramText);
            var minText = string.Format("{0:0.000} ms ", workerResult.Min);
            var maxText = string.Format(" {0:0.000} ms", workerResult.Max);
            text += "\r\n" + minText + new string('=', workerResult.Histogram.Length - minText.Length - maxText.Length) + maxText;
            return text;
        }

        private const string HelpString = @"
Usage: netling [-t threads] [-d duration] [-w warmupSec] -n plugin url

Options:
    -t count        Number of threads to spawn.
    -d count        Duration of the run in seconds.
    -w warmup       Duration of the warmup phase in seconds.
    -c count        Amount of requests to send on a single thread.
    -n pluginname   Name of plugin type to use for testing.

Examples:
    netling http://localhost:5000/
    netling http://localhost:5000/ -t 8 -d 60
    netling http://localhost:5000/ -c 3000
";

        private const string StartRunWithCountString = @"
Running {0} test @ {1}";

        private const string StartRunWithDurationString = @"
Running {0}s test with {2} threads @ {1}";

        private const string ResultString1 = @"
{0} requests in {1:0.##}s
    Requests/sec:   {2:0}
    Bandwidth:      {3:0} mbit
    Errors:         {4:0}";

        private const string ResultString2 = @"
Latency
    Median:         {0:0.000} ms
    StdDev:         {1:0.000} ms
    Min:            {2:0.000} ms
    Max:            {3:0.000} ms
    P99:            {4:0.000} ms
    P95:            {5:0.000} ms
    P50:            {6:0.000} ms

{7}
";
    }
}
