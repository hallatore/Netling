using System;

namespace Netling.Core.Models
{
    public struct UrlResult : IResult
    {
        public long Bytes { get; private set; }
        public bool IsError { get; private set; }

        public double ResponseTime { get; private set; }
        public DateTime StartTime { get; private set; }
        public string Url { get; private set; }
        public int ThreadId { get; private set; }

        public UrlResult(double responseTime, long length, DateTime startTime, string url, int threadId) : this()
        {
            ResponseTime = responseTime;
            Bytes = length;
            StartTime = startTime;
            Url = url;
            ThreadId = threadId;
        }

        public UrlResult(DateTime startTime, string url) : this()
        {
            IsError = true;
            StartTime = startTime;
            Url = url;
        }
    }
}