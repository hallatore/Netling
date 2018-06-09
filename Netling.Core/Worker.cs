using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Netling.Core.Models;

namespace Netling.Core
{
    public class Worker
    {
        private readonly IWorkerJob _workerJob;

        public Worker(IWorkerJob workerJob)
        {
            _workerJob = workerJob;
        }

        public Task<WorkerResult> Run(int threads, TimeSpan duration, CancellationToken cancellationToken)
        {
            return Run(threads, duration, null, cancellationToken);
        }

        public Task<WorkerResult> Run(int count, CancellationToken cancellationToken)
        {
            return Run(1, TimeSpan.MaxValue, count, cancellationToken);
        }

        private Task<WorkerResult> Run(int threads, TimeSpan duration, int? count, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var combinedWorkerThreadResult = QueueWorkerThreads(threads, duration, count, cancellationToken);
                var workerResult = new WorkerResult(threads, combinedWorkerThreadResult.Elapsed);
                workerResult.Process(combinedWorkerThreadResult);
                return workerResult;
            });
        }

        private CombinedWorkerThreadResult QueueWorkerThreads(int threads, TimeSpan duration, int? count, CancellationToken cancellationToken)
        {
            var results = new ConcurrentQueue<WorkerThreadResult>();
            var events = new List<ManualResetEventSlim>();
            var sw = new Stopwatch();
            sw.Start();

            for (var i = 0; i < threads; i++)
            {
                var resetEvent = new ManualResetEventSlim(false);

                Thread thread;

                if (count.HasValue)
                    thread = new Thread(async (index) => await DoWork_Count(count.Value, results, cancellationToken, resetEvent, (int)index));
                else
                    thread = new Thread(async (index) => await DoWork_Duration(duration, sw, results, cancellationToken, resetEvent, (int)index));
                
                thread.Start(i);
                events.Add(resetEvent);
            }

            for (var i = 0; i < events.Count; i += 50)
            {
                var group = events.Skip(i).Take(50).Select(r => r.WaitHandle).ToArray();
                WaitHandle.WaitAll(group);
            }

            return new CombinedWorkerThreadResult(results, sw.Elapsed);
        }

        private async Task DoWork_Duration(TimeSpan duration, Stopwatch sw, ConcurrentQueue<WorkerThreadResult> results, CancellationToken cancellationToken, ManualResetEventSlim resetEvent, int workerIndex)
        {
            IWorkerJob job;
            var workerThreadResult = new WorkerThreadResult();

            try
            {
                job = await _workerJob.Init(workerIndex, workerThreadResult);
            }
            catch (Exception)
            {
                workerThreadResult.AddError((int)sw.ElapsedMilliseconds, 0, false);
                results.Enqueue(workerThreadResult);
                resetEvent.Set();
                return;
            }

            while (!cancellationToken.IsCancellationRequested && duration.TotalMilliseconds > sw.Elapsed.TotalMilliseconds)
            {
                try
                {
                    await job.DoWork();
                }
                catch (Exception)
                {
                    workerThreadResult.AddError((int)sw.ElapsedMilliseconds, 0, false);
                }
            }
            
            results.Enqueue(job.GetResults());
            resetEvent.Set();
        }

        private async Task DoWork_Count(int count, ConcurrentQueue<WorkerThreadResult> results, CancellationToken cancellationToken, ManualResetEventSlim resetEvent, int workerIndex)
        {
            var workerThreadResult = new WorkerThreadResult();
            var job = await _workerJob.Init(workerIndex, workerThreadResult);

            for (var i = 0; i < count && !cancellationToken.IsCancellationRequested; i++)
            {
                await job.DoWork();
            }

            results.Enqueue(job.GetResults());
            resetEvent.Set();
        }
    }
}
