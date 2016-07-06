namespace Netling.Core.Models
{
    public class UrlResult
    {
        public long Bytes { get; private set; }
        public bool IsError { get; private set; }
        public double ResponseTime { get; private set; }
        public double Elapsed { get; private set; }

        public UrlResult(double elapsed, double responseTime, long length)
        {
            Bytes = length;
            ResponseTime = responseTime;
            Elapsed = elapsed;
        }

        public UrlResult(double elapsed)
        {
            IsError = true;
            Elapsed = elapsed;
        }
    }
}