using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Netling.Core.Models;
using Netling.Core.Collections;

namespace Netling.Core
{
    public class Worker
    {
        private readonly IWorkerJob _workerJob;
        private readonly Stream<Uri> _uris;

        public Worker(IWorkerJob workerJob, Stream<Uri> uris)
        {
            _workerJob = workerJob;
            _uris = uris;
        }

        public Task<WorkerResult> RunDuration(string name, int threads, TimeSpan duration, CancellationToken cancellationToken)
        {
            return RunDurationImpl(name, threads, duration, cancellationToken);
        }

        private Task<WorkerResult> RunDurationImpl(string name, int threads, TimeSpan duration, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var combinedWorkerThreadResult = QueueDurationThreads(threads, duration, cancellationToken);
                var workerResult = new WorkerResult(name, threads, combinedWorkerThreadResult.Elapsed);
                workerResult.Process(combinedWorkerThreadResult);
                return workerResult;
            });
        }

        private CombinedWorkerThreadResult Sink(ConcurrentQueue<WorkerThreadResult> results, IEnumerable<ManualResetEventSlim> events, Stopwatch sw)
        {
            for (var i = 0; i < events.Count(); i += 50)
            {
                var group = events.Skip(i).Take(50).Select(r => r.WaitHandle).ToArray();
                WaitHandle.WaitAll(group);
            }

            return new CombinedWorkerThreadResult(results, sw.Elapsed);
        }

        private CombinedWorkerThreadResult QueueDurationThreads(int threads, TimeSpan duration, CancellationToken cancellationToken)
        {
            var results = new ConcurrentQueue<WorkerThreadResult>();
            var events = new List<ManualResetEventSlim>();
            var sw = Stopwatch.StartNew();
            var bc = new BlockingCollection<Uri>();
            var feed = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (bc.Count < 5000)
                    {
                        var next = _uris.Take(1000);
                        foreach (var n in next)
                        {
                            bc.Add(n);
                        }
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
            });

            for (var i = 0; i < threads; i++)
            {
                var resetEvent = new ManualResetEventSlim(false);

                var thread = new Thread(async (index) => await DoWork_Duration(duration, sw, results, cancellationToken, resetEvent, (int)index, bc));

                thread.Start(i);
                events.Add(resetEvent);
            }

            return Sink(results, events, sw);
        }

        private async Task DoWork_Duration(TimeSpan duration, Stopwatch sw, ConcurrentQueue<WorkerThreadResult> results, CancellationToken cancellationToken, ManualResetEventSlim resetEvent, int workerIndex, BlockingCollection<Uri> stream)
        {
            IWorkerJob job;
            var workerThreadResult = new WorkerThreadResult();

            try
            {
                job = await _workerJob.Init(workerIndex, workerThreadResult);
            }
            catch (Exception ex)
            {
                workerThreadResult.AddError((int)sw.ElapsedMilliseconds / 1000, 0, 0, false, ex);
                results.Enqueue(workerThreadResult);
                resetEvent.Set();
                return;
            }

            while (!cancellationToken.IsCancellationRequested && duration.TotalMilliseconds > sw.Elapsed.TotalMilliseconds)
            {
                try
                {
                    var uri = stream.Take(cancellationToken);
                    await job.DoWork(uri);
                }
                catch (Exception ex)
                {
                    workerThreadResult.AddError((int)sw.ElapsedMilliseconds / 1000, 0, 0, false, ex);
                }
            }

            results.Enqueue(job.GetResults());
            resetEvent.Set();
        }

        public Task<WorkerResult> RunCount(string name, CancellationToken cancellationToken, int count = 1, int threads = 1)
        {
            return RunCountImpl(name, threads, count, cancellationToken);
        }

        private Task<WorkerResult> RunCountImpl(string name, int threads, int count, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var combinedWorkerThreadResult = QueueCountThreads(threads, count, cancellationToken);
                var workerResult = new WorkerResult(name, threads, combinedWorkerThreadResult.Elapsed);
                workerResult.Process(combinedWorkerThreadResult);
                return workerResult;
            });
        }

        private CombinedWorkerThreadResult QueueCountThreads(int threads, int count, CancellationToken cancellationToken)
        {
            var results = new ConcurrentQueue<WorkerThreadResult>();
            var events = new List<ManualResetEventSlim>();
            var sw = Stopwatch.StartNew();
            var bc = new BlockingCollection<Uri>();
            var feed = Task.Run(async () =>
            {
                var num = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (bc.Count < _uris.Count)
                    {
                        foreach (var n in _uris.Once())
                        {
                            bc.Add(n);
                        }
                        num++;
                    }
                    if (num >= count)
                    {
                        bc.CompleteAdding();
                        break;
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
            });

            for (var i = 0; i < threads; i++)
            {
                var resetEvent = new ManualResetEventSlim(false);

                var thread = new Thread(async (index) => await DoWork_Count(results, cancellationToken, resetEvent, (int)index, bc));

                thread.Start(i);
                events.Add(resetEvent);
            }

            return Sink(results, events, sw);
        }

        private async Task DoWork_Count(ConcurrentQueue<WorkerThreadResult> results, CancellationToken cancellationToken, ManualResetEventSlim resetEvent, int workerIndex, BlockingCollection<Uri> stream)
        {
            var workerThreadResult = new WorkerThreadResult();
            var job = await _workerJob.Init(workerIndex, workerThreadResult);

            while (!stream.IsCompleted && stream.TryTake(out var uri, -1, cancellationToken))
            {
                await job.DoWork(uri);
            }

            results.Enqueue(job.GetResults());
            resetEvent.Set();
        }
    }
}