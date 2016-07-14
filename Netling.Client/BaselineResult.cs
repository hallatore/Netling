using System;
using System.Linq;
using Netling.Client.Extensions;
using Netling.Core.Models;

namespace Netling.Client
{
    public class BaselineResult
    {
        public static BaselineResult Parse(JobResult result)
        {
            var tmp = new BaselineResult
            {
                Url = result.Url,
                Threads = result.Threads,
                Pipelining = result.Pipelining,
                ThreadAfinity = result.ThreadAfinity,

                JobsPerSecond = result.JobsPerSecond,
                ElapsedSeconds = result.ElapsedMilliseconds / 1000,
                Bandwidth = Math.Round(result.BytesPrSecond * 8 / 1024 / 1024, MidpointRounding.AwayFromZero),
                Errors = result.Errors,
            };

            if (result.ResponseTimes.Any())
            {
                tmp.Median = result.ResponseTimes.GetMedian();
                tmp.StdDev = result.ResponseTimes.GetStdDev();
                tmp.Min = result.ResponseTimes.Min();
                tmp.Max = result.ResponseTimes.Max();
            }

            return tmp;
        }

        public long Errors { get; set; }
        public double Bandwidth { get; set; }
        public double ElapsedSeconds { get; set; }
        public double JobsPerSecond { get; set; }
        public bool ThreadAfinity { get; set; }
        public int Pipelining { get; set; }
        public int Threads { get; set; }
        public string Url { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }
        public double StdDev { get; set; }
        public double Median { get; set; }
    }
}