using Netling.Core.Models;

namespace Netling.Client
{
    public class ResultWindowItem
    {
        public static ResultWindowItem Parse(WorkerResult result)
        {
            return new ResultWindowItem
            {
                Url = result.Url,
                Threads = result.Threads,
                Pipelining = result.Pipelining,
                ThreadAffinity = result.ThreadAffinity,

                JobsPerSecond = result.RequestsPerSecond,
                ElapsedSeconds = result.Elapsed.TotalSeconds,
                Bandwidth = result.Bandwidth,
                Errors = result.Errors,

                Median =  result.Median,
                StdDev = result.StdDev,
                Min = result.Min,
                Max = result.Max,
            };
        }

        public long Errors { get; set; }
        public double Bandwidth { get; set; }
        public double ElapsedSeconds { get; set; }
        public double JobsPerSecond { get; set; }
        public bool ThreadAffinity { get; set; }
        public int Pipelining { get; set; }
        public int Threads { get; set; }
        public string Url { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }
        public double StdDev { get; set; }
        public double Median { get; set; }
    }
}