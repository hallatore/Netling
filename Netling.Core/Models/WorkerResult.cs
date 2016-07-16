using System;
using System.Collections.Generic;
using System.Linq;
using Netling.Core.Extensions;

namespace Netling.Core.Models
{
    public class WorkerResult
    {
        public WorkerResult(string url, int threads, bool threadAfinity, int pipelining, TimeSpan elapsed)
        {
            Url = url;
            Threads = threads;
            ThreadAfinity = threadAfinity;
            Pipelining = pipelining;
            Elapsed = elapsed;
        }

        public string Url { get; private set; }
        public int Threads { get; private set; }
        public bool ThreadAfinity { get; private set; }
        public int Pipelining { get; private set; }
        public TimeSpan Elapsed { get; private set; }

        public long Count { get; private set; }
        public long Errors { get; private set; }
        public double RequestsPerSecond { get; private set; }
        public double BytesPrSecond { get; private set; }

        public double[] ResponseTimes { get; private set; }
        public Dictionary<int, Second> Seconds { get; set; }

        public double Median { get; private set; }
        public double StdDev { get; private set; }
        public double Min { get; private set; }
        public double Max { get; private set; }
        public int[] Histogram { get; private set; }

        public double Bandwidth
        {
            get { return Math.Round(BytesPrSecond * 8 / 1024 / 1024, MidpointRounding.AwayFromZero); }
        }

        internal void Process(WorkerThreadResult wtResult)
        {
            Seconds = wtResult.Seconds;
            var items = wtResult.Seconds.Select(r => r.Value).DefaultIfEmpty(new Second(0)).ToList();
            Count = items.Sum(s => s.Count);
            Errors = items.Sum(s => s.ErrorCount);
            RequestsPerSecond = Count / (Elapsed.TotalMilliseconds / 1000);
            BytesPrSecond = items.Sum(s => s.Bytes) / (Elapsed.TotalMilliseconds / 1000);
            ResponseTimes = items.SelectMany(s => s.ResponseTimes).OrderBy(r => r).ToArray();

            if (!ResponseTimes.Any())
                return;

            Median = ResponseTimes.GetMedian();
            StdDev = ResponseTimes.GetStdDev();
            Min = ResponseTimes.First();
            Max = ResponseTimes.Last();
            Histogram = GenerateHistogram(ResponseTimes);
        }

        private int[] GenerateHistogram(double[] responeTimes)
        {
            var splits = 80;
            var result = new int[splits];

            if (responeTimes == null || responeTimes.Length < 2)
                return result;

            var max = responeTimes.Last();
            var min = responeTimes.First();
            var divider = (max - min) / splits;
            var step = min;
            var y = 0;

            for (var i = 0; i < splits; i++)
            {
                var count = 0;
                var stepMax = step + divider;

                if (i + 1 == splits)
                    stepMax = double.MaxValue;

                while (y < responeTimes.Length && responeTimes[y] < stepMax)
                {
                    y++;
                    count++;
                }

                result[i] = count;
                step += divider;
            }

            return result;
        }
    }
}