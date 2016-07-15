using System.Collections.Generic;

namespace Netling.Core.Models
{
    public class Second
    {
        public long Count { get; set; }
        public long ErrorCount { get; private set; }
        public long Bytes { get; private set; }
        public List<double> ResponseTimes { get; private set; }
        public int Elapsed { get; private set; }

        public Second(int elapsed)
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