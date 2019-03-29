using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Netling.Core.Models;
using Netling.Core.SocketWorker.Performance;

namespace Netling.Core.SocketWorker
{
    public class SocketWorkerJob : IWorkerJob
    {
        private readonly int _index;
        private readonly Uri _uri;
        private readonly Stopwatch _stopwatch;
        private readonly Stopwatch _localStopwatch;
        private readonly WorkerThreadResult _workerThreadResult;
        private readonly HttpWorker _httpWorker;

        public SocketWorkerJob(Uri uri)
        {
            _uri = uri;
        }

        private SocketWorkerJob(int index, Uri uri, WorkerThreadResult workerThreadResult)
        {
            _index = index;
            _uri = uri;
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            _localStopwatch = new Stopwatch();
            _workerThreadResult = workerThreadResult;
            _httpWorker = new HttpWorker(new HttpWorkerClient(uri), uri);
        }

        public ValueTask DoWork()
        {
            _localStopwatch.Restart();
            var (length, statusCode) = _httpWorker.Send();

            if (statusCode < 400)
                _workerThreadResult.Add((int)_stopwatch.ElapsedMilliseconds / 1000, length, (float) _localStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000, statusCode, _index < 10);
            else
                _workerThreadResult.AddError((int)_stopwatch.ElapsedMilliseconds / 1000, (float) _localStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000, statusCode, _index < 10);

            return new ValueTask();
        }

        public WorkerThreadResult GetResults()
        {
            return _workerThreadResult;
        }

        public ValueTask<IWorkerJob> Init(int index, WorkerThreadResult workerThreadResult)
        {
            return new ValueTask<IWorkerJob>(new SocketWorkerJob(index, _uri, workerThreadResult));
        }
    }
}