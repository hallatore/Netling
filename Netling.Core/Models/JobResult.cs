using System.Collections.Generic;
using System.Linq;

namespace Netling.Core.Models
{
    public class JobResult
    {
        public int Threads { get; private set; }
        public int Count { get; private set; }
        public double ElapsedMilliseconds { get; private set; }
        public int Errors { get; private set; }
        public List<UrlResult> Results { get; private set; }
        public double BytesPrSecond { get; private set; }
        public double JobsPerSecond { get; private set; }

        public JobResult(int threads, double elapsedMilliseconds, List<UrlResult> results)
        {
            Threads = threads;
            ElapsedMilliseconds = elapsedMilliseconds;
            Results = results;
            Count = results.Count;
            Errors = results.Count(r => r.IsError);
            BytesPrSecond = results.Sum(r2 => r2.Bytes) / (elapsedMilliseconds / 1000);
            JobsPerSecond = results.Count(r => !r.IsError) / (elapsedMilliseconds / 1000);
        }
    }
}