using System;
using System.Collections.Generic;
using System.Linq;

namespace Netling.Core.Models
{
    internal class WorkerThreadResult
    {
        public Dictionary<int, Second> Seconds { get; private set; }
        public TimeSpan Elapsed { get; private set; }

        public WorkerThreadResult()
        {
            Seconds = new Dictionary<int, Second>();
        }

        public void Add(int elapsed, long bytes, double responsetime)
        {
            GetItem(elapsed).Add(bytes, responsetime);
        }

        public void AddError(int elapsed, double responsetime)
        {
            GetItem(elapsed).AddError(responsetime);
        }

        private Second GetItem(int elapsed)
        {
            if (Seconds.ContainsKey(elapsed))
                return Seconds[elapsed];

            var second = new Second(elapsed);
            Seconds.Add(elapsed, second);
            return second;
        }

        private void AddMerged(int elapsed, long bytes, List<double> responseTimes, long count, long errorCount)
        {
            GetItem(elapsed).AddMerged(bytes, responseTimes, count, errorCount);
        }

        public static WorkerThreadResult MergeResults(IReadOnlyList<WorkerThreadResult> results, TimeSpan elapsed)
        {
            var result = new WorkerThreadResult();
            result.Elapsed = elapsed;
            var tmp = results.SelectMany(c => c.Seconds).ToList();

            foreach (var item in tmp)
            {
                result.AddMerged(item.Value.Elapsed, item.Value.Bytes, item.Value.ResponseTimes, item.Value.Count, item.Value.ErrorCount);
            }

            return result;
        }
    }
}
