using System;
using System.Threading.Tasks;
using Netling.Core.Models;

namespace Netling.Core
{
    public interface IWorkerJob
    {
        void Initialize(Uri uri);

        ValueTask<IWorkerJob> Init(int index, WorkerThreadResult workerThreadResult);

        ValueTask DoWork();

        WorkerThreadResult GetResults();
    }
}