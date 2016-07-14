using System.Collections.Generic;
using System.Linq;

namespace Netling.Core.Models
{
    public class JobResult
    {
        public Dictionary<int, Item> Seconds { get; private set; }
        public long Count { get; private set; }
        public double ElapsedMilliseconds { get; private set; }
        public long Errors { get; private set; }
        public double BytesPrSecond { get; private set; }
        public double JobsPerSecond { get; private set; }
        public List<double> ResponseTimes { get; private set; }
        public string Url { get; set; }
        public int Threads { get; set; }
        public bool ThreadAfinity { get; set; }
        public int Pipelining { get; set; }

        private void GenerateSummary(double elapsedMilliseconds)
        {
            var items = Seconds.Select(r => r.Value).DefaultIfEmpty(new Item(0)).ToList();
            ElapsedMilliseconds = elapsedMilliseconds;
            Count = items.Sum(s => s.Count);
            Errors = items.Sum(s => s.ErrorCount);
            BytesPrSecond = items.Sum(s => s.Bytes) / (elapsedMilliseconds / 1000);
            JobsPerSecond = Count / (elapsedMilliseconds / 1000);
            ResponseTimes = items.SelectMany(s => s.ResponseTimes).ToList();
        }

        public JobResult()
        {
            Seconds = new Dictionary<int, Item>();
        }

        public void Add(int elapsed, long bytes, double responsetime)
        {
            GetItem(elapsed).Add(bytes, responsetime);
        }

        public void AddError(int elapsed, double responsetime)
        {
            GetItem(elapsed).AddError(responsetime);
        }

        private Item GetItem(int elapsed)
        {
            if (Seconds.ContainsKey(elapsed))
                return Seconds[elapsed];

            var item = new Item(elapsed);
            Seconds.Add(elapsed, item);
            return item;
        }

        private void AddMerged(int elapsed, long bytes, List<double> responseTimes, long count, long errorCount)
        {
            GetItem(elapsed).AddMerged(bytes, responseTimes, count, errorCount);
        }

        public static JobResult Merge(double elapsedMilliseconds, IReadOnlyList<JobResult> results)
        {
            var result = new JobResult();
            var tmp = results.SelectMany(c => c.Seconds).ToList();

            foreach (var item in tmp)
            {
                result.AddMerged(item.Value.Elapsed, item.Value.Bytes, item.Value.ResponseTimes, item.Value.Count, item.Value.ErrorCount);
            }

            result.GenerateSummary(elapsedMilliseconds);
            return result;
        }
    }

    public class Item
    {
        public long Count { get; set; }
        public long ErrorCount { get; private set; }
        public long Bytes { get; private set; }
        public List<double> ResponseTimes { get; private set; }
        public int Elapsed { get; private set; }

        public Item(int elapsed)
        {
            Elapsed = elapsed;
            ResponseTimes = new List<double>();
        }

        public void Add(long bytes, double responseTime)
        {
            Count++;
            Bytes += bytes;
            ResponseTimes.Add(responseTime);
        }

        public void AddError(double responseTime)
        {
            Count++;
            ErrorCount++;
            ResponseTimes.Add(responseTime);
        }

        public void AddMerged(long bytes, List<double> responseTimes, long count, long errorCount)
        {
            Count += count;
            ErrorCount += errorCount;
            Bytes += bytes;
            ResponseTimes.AddRange(responseTimes);
        }
    }
}
