using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine.Options;
using Netling.Core;
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
            int rampUps = 1;

            var p = new OptionSet()
            {
                {"t|threads=", (int v) => threads = v},
                {"d|duration=", (int v) => duration = v},
                {"c|count=", (int? v) => count = v},
                {"r|rampups=", (int v) => rampUps= v}
            };

            var extraArgs = p.Parse(args);
            var url = extraArgs.FirstOrDefault(e => e.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || e.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
            Uri uri = null;

            if (url != null && !Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                Console.WriteLine("Failed to parse URL");
            }
            else if (url != null && count.HasValue)
            {
                RunWithCount(uri, count.Value).Wait();
            }
            else if (rampUps > 1)
            {
                RunWithRampingUp(uri, threads, TimeSpan.FromSeconds(duration), rampUps).Wait();
            }
            else if (url != null)
            {
                RunWithDuration(uri, threads, TimeSpan.FromSeconds(duration)).Wait();
            }
            else
            {
                ShowHelp();
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine(HelpString);
        }

        private static Task RunWithCount(Uri uri, int count)
        {
            Console.WriteLine(StartRunWithCountString, count, uri);
            return Run(uri, 1, TimeSpan.MaxValue, count);
        }

        private static Task RunWithDuration(Uri uri, int threads, TimeSpan duration)
        {
            Console.WriteLine(StartRunWithDurationString, duration.TotalSeconds, uri, threads);
            return Run(uri, threads, duration, null);
        }

        private static async Task RunWithRampingUp(Uri uri, int threads, TimeSpan duration, int rampUps)
        {
            var bestResult = new WorkerResult("", 0, TimeSpan.MaxValue);
            var notRampedUpCount = 0;

            for (int iteration = 0; iteration < rampUps; iteration++)
            {
                Console.WriteLine(StartRunWithDurationString, duration.TotalSeconds, uri, threads);
                var result = await Run(uri, threads, duration, null);
                if (iteration == 0)
                    bestResult = result;

                if (result.RequestsPerSecond >= bestResult.RequestsPerSecond && result.Errors <= bestResult.Errors)
                {
                    bestResult = result;
                    threads++;
                    notRampedUpCount = 0;
                }
                else
                {
                    notRampedUpCount++;
                }

                if (notRampedUpCount == 3)
                {
                    Console.WriteLine("Unable to increase threads without reducing req/sec or increasing errors after 3 attempts - stopping test");
                    break;
                }
            }

            if (rampUps > 1)
            {
                Console.WriteLine(ResultString,
                    $"Ramp Up identified the following as the best result with {threads} threads\n{bestResult.Count}",
                    bestResult.Elapsed.TotalSeconds,
                    bestResult.RequestsPerSecond,
                    bestResult.Bandwidth,
                    bestResult.Errors,
                    bestResult.Median,
                    bestResult.StdDev,
                    bestResult.Min,
                    bestResult.Max,
                    GetAsciiHistogram(bestResult));
            }
        }

        private static async Task<WorkerResult> Run(Uri uri, int threads, TimeSpan duration, int? count)
        {
            WorkerResult result;
            var worker = new Worker(new SocketWorkerJob(uri));

            if (count.HasValue)
            {
                result = await worker.Run(uri.ToString(), count.Value, new CancellationToken());
            }
            else
            {
                result = await worker.Run(uri.ToString(), threads, duration, new CancellationToken());
            }

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
            return result;
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
Usage: netling [-t threads] [-d duration] url

Options:
    -t count        Number of threads to spawn.
    -d count        Duration of the run in seconds.
    -c count        Amount of requests to send on a single thread.
    -r rampups      How many times to repeat the test while trying to increase the number of threads

Examples:
    netling http://localhost:5000/
    netling http://localhost:5000/ -t 8 -d 60
    netling http://localhost:5000/ -c 3000
    netling http://localhost:5000/ -t 8 -d 60 -r 10
";

        private const string StartRunWithCountString = @"
Running {0} test @ {1}";

        private const string StartRunWithDurationString = @"
Running {0}s test with {2} threads @ {1}";

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
