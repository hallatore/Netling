using System;
using System.Collections.Generic;
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
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");

            var threads = 1;
            var pipelining = 1;
            var duration = 10;

            var p = new OptionSet()
            {
                {"t|threads=", (int v) => threads = v},
                {"p|pipelining=", (int v) => pipelining = v},
                {"d|duration=", (int v) => duration = v},
            };

            var extraArgs = p.Parse(args);
            var threadAfinity = extraArgs.Contains("-a");
            var url = extraArgs.FirstOrDefault(e => e.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || e.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
            Uri uri = null;

            if (url != null && !Uri.TryCreate(url, UriKind.Absolute, out uri))
                Console.WriteLine("Failed to parse URL");
            else if (url != null)
                Run(uri, threads, threadAfinity, pipelining, TimeSpan.FromSeconds(duration)).Wait();
            else
                ShowHelp();
        }

        private static void ShowHelp()
        {
            Console.WriteLine(HelpString);
        }

        private static async Task Run(Uri uri, int threads, bool threadAfinity, int pipelining, TimeSpan duration)
        {
            Console.WriteLine(StartRunString, duration.TotalSeconds, uri, threads, pipelining, threadAfinity ? "ON" : "OFF");
            var result = await Worker.Run(uri, threads, threadAfinity, pipelining, duration, new CancellationToken());

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
    -p count        Number of requests to pipeline.
    -a              Use thread afinity on the worker threads.

Examples: 
    netling -t 8 -d 60 http://localhost
    netling http://localhost
";

        private const string StartRunString = @"
Running {0}s test @ {1}
    Threads:        {2}
    Pipelining:     {3}
    Thread afinity: {4}";

        private const string ResultString = @"
{0:#,0} requests in {1:#,0.##}s
    Requests/sec:   {2:#,0}
    Bandwidth:      {3:#,0} mbit
    Errors:         {4:#,0}
Latency
    Median:         {5:0.000} ms
    StdDev:         {6:0.000} ms
    Min:            {7:0.000} ms
    Max:            {8:0.000} ms

{9}
";
    }
}
