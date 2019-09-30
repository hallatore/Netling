using System.Threading.Tasks;
using Netling.Core.Models;
using System;

namespace Netling.Core
{
    public interface IWorkerJob
    {
        ValueTask<IWorkerJob> Init(int index, WorkerThreadResult workerThreadResult);
        ValueTask DoWork();
        WorkerThreadResult GetResults();
    }
}