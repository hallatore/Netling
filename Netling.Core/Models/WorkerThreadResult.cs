using System.Collections.Generic;

namespace Netling.Core.Models
{
    public class WorkerThreadResult
    {
        public Dictionary<int, Second> Seconds { get; }

        public WorkerThreadResult()
        {
            Seconds = new Dictionary<int, Second>();
        }

        public void Add(int elapsed, long bytes, float responsetime, bool trackResponseTime)
        {
            GetItem(elapsed).Add(bytes, responsetime, trackResponseTime);
        }

        public void AddError(int elapsed, float responsetime, bool trackResponseTime)
        {
            GetItem(elapsed).AddError(responsetime, trackResponseTime);
        }

        private Second GetItem(int elapsed)
        {
            if (Seconds.ContainsKey(elapsed))
                return Seconds[elapsed];

            var second = new Second(elapsed);
            Seconds.Add(elapsed, second);
            return second;
        }
    }
}
