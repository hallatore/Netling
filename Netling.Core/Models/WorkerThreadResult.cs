using System;
using System.Collections.Generic;

namespace Netling.Core.Models
{
    public class WorkerThreadResult
    {
        public Dictionary<int, Second> Seconds { get; }
        public Dictionary<int, int> StatusCodes { get; set; }
        public Dictionary<Type,Exception> Exceptions { get; set; }

        private Second _tmpSeconds;
        private int _tmpElapsedSeconds = -1;

        public WorkerThreadResult()
        {
            Seconds = new Dictionary<int, Second>();
            StatusCodes = new Dictionary<int, int>();
            Exceptions = new Dictionary<Type, Exception>();
        }

        public void Add(int elapsedSeconds, long bytes, float responseTime, int statusCode, bool trackResponseTime)
        {
            AddOrUpdateStatusCode(statusCode);

            if (_tmpElapsedSeconds != elapsedSeconds)
            {
                _tmpSeconds = GetCurrentSecond(elapsedSeconds);
                _tmpElapsedSeconds = elapsedSeconds;
            }
                
            _tmpSeconds.Add(bytes, responseTime, trackResponseTime);
        }

        public void AddError(int elapsedSeconds, float responseTime, int statusCode, bool trackResponseTime, Exception exception = null)
        {
            AddOrUpdateStatusCode(statusCode);
            GetCurrentSecond(elapsedSeconds).AddError(responseTime, trackResponseTime);

            if (exception != null && !Exceptions.ContainsKey(exception.GetType()))
            {
                Exceptions.Add(exception.GetType(), exception);
            }
        }

        private void AddOrUpdateStatusCode(int statusCode)
        {
            if (statusCode == 0)
            {
                return;
            }

            if (!StatusCodes.ContainsKey(statusCode))
            {
                StatusCodes.Add(statusCode, 0);
            }

            StatusCodes[statusCode]++;
        }

        private Second GetCurrentSecond(int elapsedSeconds)
        {
            if (Seconds.ContainsKey(elapsedSeconds))
            {
                return Seconds[elapsedSeconds];
            }

            var second = new Second();
            Seconds.Add(elapsedSeconds, second);
            return second;
        }
    }
}
