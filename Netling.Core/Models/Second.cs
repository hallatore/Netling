using System;
using System.Collections.Generic;

namespace Netling.Core.Models
{
    public class Second
    {
        public long Count { get; set; }
        public long Bytes { get; private set; }
        public List<double> ResponseTimes { get; private set; }
        public Dictionary<int, int> StatusCodes { get; private set; }
        public Dictionary<Type, int> Exceptions { get; private set; }
        public int Elapsed { get; private set; }

        public Second(int elapsed)
        {
            Elapsed = elapsed;
            ResponseTimes = new List<double>();
            StatusCodes = new Dictionary<int, int>();
            Exceptions = new Dictionary<Type, int>();
        }

        internal void ClearResponseTimes()
        {
            ResponseTimes = new List<double>();
        }

        public void Add(long bytes, double responseTime, int statusCode, bool trackResponseTime)
        {
            Count++;
            Bytes += bytes;

            if (trackResponseTime)
                ResponseTimes.Add(responseTime);

            if (StatusCodes.ContainsKey(statusCode))
                StatusCodes[statusCode]++;
            else
                StatusCodes.Add(statusCode, 1);
        }

        public void AddError(double responseTime, Exception exception)
        {
            Count++;
            ResponseTimes.Add(responseTime);

            var exceptionType = exception.GetType();
            if (Exceptions.ContainsKey(exceptionType))
                Exceptions[exceptionType]++;
            else
                Exceptions.Add(exceptionType, 1);
        }

        public void AddMerged(Second second)
        {
            Count += second.Count;
            Bytes += second.Bytes;

            foreach (var statusCode in second.StatusCodes)
            {
                if (StatusCodes.ContainsKey(statusCode.Key))
                    StatusCodes[statusCode.Key] += statusCode.Value;
                else
                    StatusCodes.Add(statusCode.Key, statusCode.Value);
            }

            foreach (var exception in second.Exceptions)
            {
                if (Exceptions.ContainsKey(exception.Key))
                    Exceptions[exception.Key] += exception.Value;
                else
                    Exceptions.Add(exception.Key, exception.Value);
            }
        }
    }
}