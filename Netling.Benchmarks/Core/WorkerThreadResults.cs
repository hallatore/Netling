using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Netling.Core;
using Netling.Core.Models;
using Netling.Core.SocketWorker;

namespace Netling.Benchmarks.Core
{
    [MemoryDiagnoser]
    public class WorkerThreadResults
    {
        private readonly WorkerThreadResult _result;
        private IWorkerJob _worker;

        public WorkerThreadResults()
        {
            _result = new WorkerThreadResult();
            var socketWorker = new SocketWorkerJob(new Uri("http://localhost:5000"));
            _worker = socketWorker.Init(0, _result).Result;
        }

        //[Benchmark]
        public void Add()
        {
            for (var i = 0; i < 1000; i++)
            {
                _result.Add(i / 100, 1337, 10f, 200, false);
            }
        }

        [Benchmark]
        public async ValueTask SocketWorker()
        {
            await _worker.DoWork();
        }
    }
}
