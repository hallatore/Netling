using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Netling.Core.Models
{
    public class CombinedWorkerThreadResult
    {
        public Dictionary<int, Second> Seconds { get; private set; }
        public Dictionary<int, int> StatusCodes { get; private set; }
        public List<List<float>> ResponseTimes { get; private set; }
        public Dictionary<Type, Exception> Exceptions { get; set; }
        public TimeSpan Elapsed { get; private set; }

        public CombinedWorkerThreadResult(ConcurrentQueue<WorkerThreadResult> results, TimeSpan elapsed)
        {
            Seconds = new Dictionary<int, Second>();
            StatusCodes = new Dictionary<int, int>();
            ResponseTimes = new List<List<float>>();
            Exceptions = new Dictionary<Type, Exception>();
            Elapsed = elapsed;

            foreach (var result in results)
            {
                foreach (var statusCode in result.StatusCodes)
                {
                    if (!StatusCodes.ContainsKey(statusCode.Key))
                        StatusCodes.Add(statusCode.Key, 0);

                    StatusCodes[statusCode.Key] += statusCode.Value;
                }

                foreach (var exception in result.Exceptions)
                {
                    if (!Exceptions.ContainsKey(exception.Key))
                        Exceptions.Add(exception.Key, exception.Value);
                }

                foreach (var second in result.Seconds)
                {
                    ResponseTimes.Add(second.Value.ResponseTimes);
                    second.Value.ClearResponseTimes();
                    
                    if (Seconds.ContainsKey(second.Key))
                        Seconds[second.Key].AddMerged(second.Value);
                    else
                        Seconds.Add(second.Key, second.Value);
                }
            }
        }
    }
}