using System;
using System.Collections.Generic;
using System.Text;

namespace Netling.Core
{
    public interface IScaleOptions
    {
        public int Threads { get; set; }
        public int Concurrency { get; set; }
    }

    public struct DurationOptions : IScaleOptions
    {
        public int Threads { get; set; }
        public TimeSpan Duration { get; set; }
        public int Concurrency { get; set; }

        public static DurationOptions Default
        {
            get
            {
                return new DurationOptions
                {
                    Threads = 1,
                    Duration = TimeSpan.FromSeconds(20),
                    Concurrency = 1
                };
            }
        }
    }

    public struct CountOptions : IScaleOptions
    {
        public int Threads { get; set; }
        public int Count { get; set; }
        public int Concurrency { get; set; }

        public static CountOptions Default
        {
            get
            {
                return new CountOptions
                {
                    Threads = 1,
                    Count = 1,
                    Concurrency = 1
                };
            }
        }
    }
}
