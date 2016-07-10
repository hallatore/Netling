using System;
using System.Threading;
using Netling.Core.Models;

namespace Netling.Core
{
    public interface IJob
    {
        JobResult Process(int threads, bool threadAfinity, int pipelining, TimeSpan duration, string url, CancellationToken cancellationToken);
    }
}