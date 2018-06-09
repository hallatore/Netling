using System.Collections.Generic;

namespace Netling.Core.Models
{
    public class Second
    {
        public long Count { get; set; }
        public long Bytes { get; private set; }
        public long Errors { get; private set; }
        public int Elapsed { get; }
        public List<float> ResponseTimes { get; private set; }

        public Second(int elapsed)
        {
            Elapsed = elapsed;
            ResponseTimes = new List<float>();
        }

        internal void ClearResponseTimes()
        {
            ResponseTimes = new List<float>();
        }

        public void Add(long bytes, float responseTime, bool trackResponseTime)
        {
            Count++;
            Bytes += bytes;

            if (trackResponseTime)
                ResponseTimes.Add(responseTime);
        }

        public void AddError(float responseTime, bool trackResponseTime)
        {
            Count++;
            Errors++;

            if (trackResponseTime)
                ResponseTimes.Add(responseTime);
        }

        public void AddMerged(Second second)
        {
            Count += second.Count;
            Bytes += second.Bytes;
            Errors += second.Errors;
        }
    }
}