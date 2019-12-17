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

        public Task<WorkerResult> Run(string name, int threads, TimeSpan duration, TimeSpan warmupDuration, CancellationToken cancellationToken)
        {
            return Run(name, threads, duration, warmupDuration, null, cancellationToken);
        }

        public Task<WorkerResult> Run(string name, int count, TimeSpan warmupDuration, CancellationToken cancellationToken)
        {
            return Run(name, 1, TimeSpan.MaxValue, warmupDuration, count, cancellationToken);
        }

        private Task<WorkerResult> Run(string name, int threads, TimeSpan duration, TimeSpan warmupDuration, int? count, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var combinedWorkerThreadResult = QueueWorkerThreads(threads, duration, warmupDuration, count, cancellationToken);
                var workerResult = new WorkerResult(name, threads, combinedWorkerThreadResult.Elapsed);
                workerResult.Process(combinedWorkerThreadResult);
                return workerResult;
            });
        }

        private CombinedWorkerThreadResult QueueWorkerThreads(int threads, TimeSpan duration, TimeSpan warmupDuration, int? count, CancellationToken cancellationToken)
        {
            var results = new ConcurrentQueue<WorkerThreadResult>();
            var events = new List<ManualResetEventSlim>();
            var sw = Stopwatch.StartNew();

            var startWorkEvent = new ManualResetEventSlim(false);

            for (var i = 0; i < threads; i++)
            {
                var resetEvent = new ManualResetEventSlim(false);

                Thread thread;

                if (count.HasValue)
                {
                    thread = new Thread(async (index) => await DoWork_Count(count.Value, results, warmupDuration, sw, cancellationToken, startWorkEvent, resetEvent, (int)index));
                }
                else
                {
                    thread = new Thread(async (index) => await DoWork_Duration(duration, warmupDuration, sw, results, cancellationToken, startWorkEvent, resetEvent, (int)index));
                }

                thread.Start(i);
                events.Add(resetEvent);

                Thread.Sleep(100);
            }

            sw.Restart();

            // start the work on all threads
            startWorkEvent.Set();


            for (var i = 0; i < events.Count; i += 50)
            {
                var group = events.Skip(i).Take(50).Select(r => r.WaitHandle).ToArray();
                WaitHandle.WaitAll(group);
            }

            return new CombinedWorkerThreadResult(results, sw.Elapsed - warmupDuration);
        }

        private async Task DoWork_Duration(
            TimeSpan duration, 
            TimeSpan warmupDuration, 
            Stopwatch sw, 
            ConcurrentQueue<WorkerThreadResult> results, 
            CancellationToken cancellationToken,
            ManualResetEventSlim startWorkEvent,
            ManualResetEventSlim resetEvent, 
            int workerIndex)
        {
            IWorkerJob job;
            var workerThreadResult = new WorkerThreadResult();

            try
            {
                job = await _workerJob.Init(workerIndex, workerThreadResult);
                await job.DoWork();
            }
            catch (Exception ex)
            {
                workerThreadResult.AddError((int)sw.ElapsedMilliseconds / 1000, 0, 0, false, ex);
                results.Enqueue(workerThreadResult);
                resetEvent.Set();
                return;
            }

            startWorkEvent.Wait();

            // warmup phase
            while (!cancellationToken.IsCancellationRequested && warmupDuration.TotalMilliseconds > sw.Elapsed.TotalMilliseconds)
            {
                try
                {
                    await job.DoWork();
                }
                catch (Exception ex)
                {
                    workerThreadResult.AddError((int)sw.ElapsedMilliseconds / 1000, 0, 0, false, ex);
                }
            }

            workerThreadResult.Clear();

            // main phase
            while (!cancellationToken.IsCancellationRequested && (duration.TotalMilliseconds + warmupDuration .TotalMilliseconds)> sw.Elapsed.TotalMilliseconds)
            {
                try
                {
                    await job.DoWork();
                }
                catch (Exception ex)
                {
                    workerThreadResult.AddError((int)sw.ElapsedMilliseconds / 1000, 0, 0, false, ex);
                }
            }

            results.Enqueue(job.GetResults());
            resetEvent.Set();
        }

        private async Task DoWork_Count(
            int count, 
            ConcurrentQueue<WorkerThreadResult> results, 
            TimeSpan warmupDuration, 
            Stopwatch sw, 
            CancellationToken cancellationToken,
            ManualResetEventSlim startWorkEvent,
            ManualResetEventSlim resetEvent, 
            int workerIndex)
        {
            var workerThreadResult = new WorkerThreadResult();
            IWorkerJob job;

            try
            {
                job = await _workerJob.Init(workerIndex, workerThreadResult);
                await job.DoWork();
            }
            catch (Exception ex)
            {
                workerThreadResult.AddError((int)sw.ElapsedMilliseconds / 1000, 0, 0, false, ex);
                results.Enqueue(workerThreadResult);
                resetEvent.Set();
                return;
            }

            startWorkEvent.Wait();

            // warmup phase
            while (!cancellationToken.IsCancellationRequested && warmupDuration.TotalMilliseconds > sw.Elapsed.TotalMilliseconds)
            {
                try
                {
                    await job.DoWork();
                }
                catch (Exception ex)
                {
                    workerThreadResult.AddError((int)sw.ElapsedMilliseconds / 1000, 0, 0, false, ex);
                }
            }

            workerThreadResult.Clear();

            // main phase
            for (var i = 0; i < count && !cancellationToken.IsCancellationRequested; i++)
            {
                await job.DoWork();
            }

            results.Enqueue(job.GetResults());
            resetEvent.Set();
        }
    }
}