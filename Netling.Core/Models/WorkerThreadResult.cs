using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Netling.Core.Models
{
    internal class WorkerThreadResult
    {
        public Dictionary<int, Second> Seconds { get; private set; }

        public WorkerThreadResult()
        {
            Seconds = new Dictionary<int, Second>();
        }

        public void Add(int elapsed, long bytes, float responsetime, int statusCode, bool trackResponseTime)
        {
            GetItem(elapsed).Add(bytes, responsetime, statusCode, trackResponseTime);
        }

        public void AddError(int elapsed, float responsetime, Exception exception)
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
    }
}
