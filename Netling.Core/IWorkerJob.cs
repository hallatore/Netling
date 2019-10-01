using System.Threading.Tasks;
using Netling.Core.Models;
using System;
using System.Collections.Concurrent;

namespace Netling.Core
{
    public interface IWorkerJob
    {
        ValueTask<IWorkerJob> Init(int index, WorkerThreadResult workerThreadResult);
        Task DoWork(Uri uri);
        WorkerThreadResult GetResults();
    }
}