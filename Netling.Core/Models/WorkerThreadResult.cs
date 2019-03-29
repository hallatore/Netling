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

        public void Add(int elapsedSeconds, long bytes, float responsetime, bool trackResponseTime)
        {
            GetItem(elapsedSeconds).Add(bytes, responsetime, trackResponseTime);
        }

        public void AddError(int elapsedSeconds, float responsetime, bool trackResponseTime)
        {
            GetItem(elapsedSeconds).AddError(responsetime, trackResponseTime);
        }

        private Second GetItem(int elapsedSeconds)
        {
            if (Seconds.ContainsKey(elapsedSeconds))
                return Seconds[elapsedSeconds];

            var second = new Second();
            Seconds.Add(elapsedSeconds, second);
            return second;
        }
    }
}
