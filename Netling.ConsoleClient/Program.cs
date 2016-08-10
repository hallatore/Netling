using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NDesk.Options;
using Netling.Core;
using Netling.Core.Models;

namespace Netling.ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en");

            var threads = 1;
            var pipelining = 1;
            var duration = 10;
            int? count = null;

            var p = new OptionSet()
            {
                {"t|threads=", (int v) => threads = v},
                {"p|pipelining=", (int v) => pipelining = v},
                {"d|duration=", (int v) => duration = v},
                {"c|count=", (int? v) => count = v},
            };

            var extraArgs = p.Parse(args);
            var threadAffinity = extraArgs.Contains("-a");
            var url = extraArgs.FirstOrDefault(e => e.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || e.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
            Uri uri = null;

            if (url != null && !Uri.TryCreate(url, UriKind.Absolute, out uri))
                Console.WriteLine("Failed to parse URL");
            else if (url != null && count.HasValue)
                RunWithCount(uri, count.Value).Wait();
            else if (url != null)
                RunWithDuration(uri, threads, threadAffinity, pipelining, TimeSpan.FromSeconds(duration)).Wait();
            else
                ShowHelp();
        }

        private static void ShowHelp()
        {
            Console.WriteLine(HelpString);
        }

        private static Task RunWithCount(Uri uri, int count)
        {
            Console.WriteLine(StartRunWithCountString, count, uri);
            return Run(uri, 1, false, 1, TimeSpan.MaxValue, count);
        }

        private static Task RunWithDuration(Uri uri, int threads, bool threadAffinity, int pipelining, TimeSpan duration)
        {
            Console.WriteLine(StartRunWithDurationString, duration.TotalSeconds, uri, threads, pipelining, threadAffinity ? "ON" : "OFF");
            return Run(uri, threads, threadAffinity, pipelining, duration, null);
        }

        private static async Task Run(Uri uri, int threads, bool threadAffinity, int pipelining, TimeSpan duration, int? count)
        {
            WorkerResult result;

            if (count.HasValue)
                result = await Worker.Run(uri, count.Value, new CancellationToken());
                    else
            result = await Worker.Run(uri, threads, threadAffinity, pipelining, duration, new CancellationToken());

            Console.WriteLine(ResultString, 
                result.Count,
                result.Elapsed.TotalSeconds,
                result.RequestsPerSecond, 
                result.Bandwidth, 
                result.Errors,
                result.Median,
                result.StdDev,
                result.Min,
                result.Max,
                GetAsciiHistogram(result));
        }

        private static string GetAsciiHistogram(WorkerResult workerResult)
        {
            if (workerResult.Histogram.Length == 0)
                return string.Empty;

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
Usage: netling [-t threads] [-d duration] [-p pipelining] [-a] url

Options:
    -t count        Number of threads to spawn.
    -d count        Duration of the run in seconds.
    -c count        Amount of requests to send on a single thread.
    -p count        Number of requests to pipeline.
    -a              Use thread affinity on the worker threads.

Examples: 
    netling http://localhost -t 8 -d 60
    netling http://localhost -c 3000 
    netling http://localhost
";

        private const string StartRunWithCountString = @"
Running {0} test @ {1}";

        private const string StartRunWithDurationString = @"
Running {0}s test @ {1}
    Threads:        {2}
    Pipelining:     {3}
    Thread affinity: {4}";

        private const string ResultString = @"
{0} requests in {1:0.##}s
    Requests/sec:   {2:0}
    Bandwidth:      {3:0} mbit
    Errors:         {4:0}
Latency
    Median:         {5:0.000} ms
    StdDev:         {6:0.000} ms
    Min:            {7:0.000} ms
    Max:            {8:0.000} ms

{9}
";
    }
}
