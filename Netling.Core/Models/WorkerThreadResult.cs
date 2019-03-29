using System;
using System.Collections.Generic;

namespace Netling.Core.Models
{
    public class WorkerThreadResult
    {
        public Dictionary<int, Second> Seconds { get; }
        public Dictionary<int, int> StatusCodes { get; set; }
        public Dictionary<Type,Exception> Exceptions { get; set; }

        public WorkerThreadResult()
        {
            Seconds = new Dictionary<int, Second>();
            StatusCodes = new Dictionary<int, int>();
            Exceptions = new Dictionary<Type, Exception>();
        }

        public void Add(int elapsedSeconds, long bytes, float responsetime, int statusCode, bool trackResponseTime)
        {
            AddOrUpdateStatusCode(statusCode);
            GetItem(elapsedSeconds).Add(bytes, responsetime, trackResponseTime);
        }

        public void AddError(int elapsedSeconds, float responsetime, int statusCode, bool trackResponseTime, Exception exception = null)
        {
            AddOrUpdateStatusCode(statusCode);
            GetItem(elapsedSeconds).AddError(responsetime, trackResponseTime);

            if (exception != null && !Exceptions.ContainsKey(exception.GetType()))
            {
                Exceptions.Add(exception.GetType(), exception);
            }
        }

        private void AddOrUpdateStatusCode(int statusCode)
        {
            if (statusCode == 0)
                return;

            if (!StatusCodes.ContainsKey(statusCode))
                StatusCodes.Add(statusCode, 0);

            StatusCodes[statusCode]++;
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
