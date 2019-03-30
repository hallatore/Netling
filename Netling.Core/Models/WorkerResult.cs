using System;
using System.Collections.Generic;
using System.Linq;
using Netling.Core.Extensions;

namespace Netling.Core.Models
{
    public class WorkerResult
    {
        public WorkerResult(string name, int threads, TimeSpan elapsed)
        {
            Name = name;
            Threads = threads;
            Elapsed = elapsed;
            Seconds = new Dictionary<int, Second>();
            Histogram = new int[0];
        }

        public string Name { get; }
        public int Threads { get; }
        public TimeSpan Elapsed { get; }

        public long Count { get; private set; }
        public long Errors { get; private set; }
        public double RequestsPerSecond { get; private set; }
        public double BytesPrSecond { get; private set; }        
        public Dictionary<int, Second> Seconds { get; private set; }
        public Dictionary<int, int> StatusCodes { get; private set; }
        public List<Exception> Exceptions { get; private set; }

        public double Median { get; private set; }
        public double StdDev { get; private set; }
        public double Min { get; private set; }
        public double Max { get; private set; }
        public int[] Histogram { get; private set; }

        public double Bandwidth => Math.Round(BytesPrSecond * 8 / 1024 / 1024, MidpointRounding.AwayFromZero);

        public void Process(CombinedWorkerThreadResult wtResult)
        {
            Seconds = wtResult.Seconds;
            StatusCodes = wtResult.StatusCodes;
            Exceptions = wtResult.Exceptions.Values.ToList();
            var items = wtResult.Seconds.Select(r => r.Value).DefaultIfEmpty(new Second()).ToList();
            Count = items.Sum(s => s.Count);
            Errors = items.Sum(s => s.Errors);
            RequestsPerSecond = Count / (Elapsed.TotalMilliseconds / 1000);
            BytesPrSecond = items.Sum(s => s.Bytes) / (Elapsed.TotalMilliseconds / 1000);

            var responseTimes = GetResponseTimes(wtResult.ResponseTimes);
            if (!responseTimes.Any())
            {
                return;
            }

            Median = responseTimes.GetMedian();
            StdDev = responseTimes.GetStdDev();
            Min = responseTimes.First();
            Max = responseTimes.Last();
            Histogram = GenerateHistogram(responseTimes);
        }

        private static float[] GetResponseTimes(List<List<float>> items)
        {
            var result = new float[items.Sum(s => s.Count)];
            var offset = 0;

            for (var i = 0; i < items.Count; i++)
            {
                items[i].CopyTo(result, offset);
                offset += items[i].Count;
                items[i] = null;
            }

            GC.Collect();
            Array.Sort(result);
            return result;
        }

        private int[] GenerateHistogram(float[] responeTimes)
        {
            var splits = 80;
            var result = new int[splits];

            if (responeTimes == null || responeTimes.Length < 2)
            {
                return result;
            }

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
                {
                    stepMax = float.MaxValue;
                }

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