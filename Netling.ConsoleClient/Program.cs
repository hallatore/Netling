using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NDesk.Options;
using Netling.Core;

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

            if (url != null)
                Run(url, threads, threadAfinity, pipelining, TimeSpan.FromSeconds(duration)).Wait();
            else
                ShowHelp();
        }

        private static void ShowHelp()
        {
            Console.WriteLine(HelpString);
        }

        private static async Task Run(string url, int threads, bool threadAfinity, int pipelining, TimeSpan duration)
        {
            Console.WriteLine(StartRunString, duration.TotalSeconds, url, threads, pipelining, threadAfinity ? "ON" : "OFF");
            var result = await Worker.Run(url, threads, threadAfinity, pipelining, duration, new CancellationToken());

            Console.WriteLine(ResultString, 
                result.Count,
                result.Elapsed.TotalSeconds,
                result.RequestsPerSecond, 
                result.Bandwidth, 
                result.Errors,
                result.Median,
                result.StdDev,
                result.Min,
                result.Max);
        }

        private const string HelpString = @"
Usage: netling [-t threads] [-d duration] [-p pipelining] [-a]

Options:
    -t count        Number of threads to spawn.
    -d count        Duration of the run in seconds.
    -p count        Number of requests to pipeline.
    -a              Use thread afinity on the worker threads.

Examples: 
    netling -t 8 -d 60 -p 10 -a http://localhost
    netling http://localhost
";

        private const string StartRunString = @"
Running {0}s test @ {1}
    Threads:        {2}
    Pipelining:     {3}
    Thread afinity: {4}";

        private const string ResultString = @"
{0} requests in {1:#,0.##}s
    Requests/sec:   {2:#,0}
    Bandwidth:      {3:#,0} mbit
    Errors:         {4:#,0}
Latency
    Median:         {5:0.000} ms
    StdDev:         {6:0.000} ms
    Min:            {7:0.000} ms
    Max:            {8:0.000} ms
";
    }
}
