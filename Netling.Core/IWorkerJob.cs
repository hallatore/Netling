using System.Threading.Tasks;
using Netling.Core.Models;

namespace Netling.Core
{
    public interface IWorkerJob
    {
        Task<IWorkerJob> Init(int index, WorkerThreadResult workerThreadResult);
        Task DoWork();
        WorkerThreadResult GetResults();
    }
}