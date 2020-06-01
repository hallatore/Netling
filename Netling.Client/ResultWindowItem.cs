using Netling.Core.Models;

namespace Netling.Client
{
    public class ResultWindowItem
    {
        public static ResultWindowItem Parse(WorkerResult result)
        {
            return new ResultWindowItem
            {
                Name = result.Name,
                Threads = result.Threads,

                JobsPerSecond = result.RequestsPerSecond,
                ElapsedSeconds = result.Elapsed.TotalSeconds,
                Bandwidth = result.Bandwidth,
                Errors = result.Errors,

                Median = result.Median,
                StdDev = result.StdDev,
                Min = result.Min,
                Max = result.Max,
                P99 = result.P99,
                P95 = result.P95,
                P50 = result.P50
            };
        }

        public string Name { get; set; }
        public int Threads { get; set; }
        public long Errors { get; set; }
        public double Bandwidth { get; set; }
        public double ElapsedSeconds { get; set; }
        public double JobsPerSecond { get; set; }
        public double Max { get; set; }
        public double P99 { get; set; }
        public double P95 { get; set; }
        public double P50 { get; set; }
        public double Min { get; set; }
        public double StdDev { get; set; }
        public double Median { get; set; }
    }
}