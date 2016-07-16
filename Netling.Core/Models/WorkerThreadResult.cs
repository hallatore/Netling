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

        public void Add(int elapsed, long bytes, double responsetime, int statusCode)
        {
            GetItem(elapsed).Add(bytes, responsetime, statusCode);
        }

        public void AddError(int elapsed, double responsetime, Exception exception)
        {
            GetItem(elapsed).AddError(responsetime, exception);
        }

        private Second GetItem(int elapsed)
        {
            if (Seconds.ContainsKey(elapsed))
                return Seconds[elapsed];

            var second = new Second(elapsed);
            Seconds.Add(elapsed, second);
            return second;
        }

        private void AddMerged(Second second)
        {
            GetItem(second.Elapsed).AddMerged(second);
        }

        public static WorkerThreadResult MergeResults(IReadOnlyList<WorkerThreadResult> results, TimeSpan elapsed)
        {
            var result = new WorkerThreadResult();
            result.Elapsed = elapsed;
            var tmp = results.SelectMany(c => c.Seconds).ToList();

            foreach (var item in tmp)
            {
                result.AddMerged(item.Value);
            }

            return result;
        }
    }
}
