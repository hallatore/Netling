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

        public Task<WorkerResult> Run(string name, DurationOptions opts, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var combinedWorkerThreadResult = QueueDurationThreads(opts, cancellationToken);
                var workerResult = new WorkerResult(name, opts.Threads, combinedWorkerThreadResult.Elapsed);
                workerResult.Process(combinedWorkerThreadResult);
                return workerResult;
            });
        }

        public Task<WorkerResult> Run(string name, CountOptions opts, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var combinedWorkerThreadResult = QueueCountThreads(opts, cancellationToken);
                var workerResult = new WorkerResult(name, opts.Threads, combinedWorkerThreadResult.Elapsed);
                workerResult.Process(combinedWorkerThreadResult);
                return workerResult;
            });
        }

        private CombinedWorkerThreadResult QueueDurationThreads(DurationOptions opts, CancellationToken cancellationToken)
        {
            var results = new ConcurrentQueue<WorkerThreadResult>();
            var events = new List<ManualResetEventSlim>();
            var sw = Stopwatch.StartNew();
            var bc = new BlockingCollection<Uri>();
            var feed = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (bc.Count < _uris.Count(opts))
                    {
                        var next = _uris.Take(opts);
                        foreach (var n in next)
                        {
                            bc.Add(n);
                        }
                    }
                    await Task.Delay(_uris.Frequency(opts));
                }
            });

            for (var i = 0; i < opts.Threads; i++)
            {
                var resetEvent = new ManualResetEventSlim(false);

                var thread = new Thread(async (index) => await DoWork_Duration(opts, sw, results, cancellationToken, resetEvent, (int)index, bc));

                thread.Start(i);
                events.Add(resetEvent);
            }

            return Sink(results, events, sw);
        }

        private async Task DoWork_Duration(DurationOptions opts, Stopwatch sw, ConcurrentQueue<WorkerThreadResult> results, CancellationToken cancellationToken, ManualResetEventSlim resetEvent, int workerIndex, BlockingCollection<Uri> stream)
        {
            IWorkerJob job;
            var workerThreadResult = new WorkerThreadResult();
            var sem = new SemaphoreSlim(opts.Concurrency);
            var bag = new ConcurrentBag<Task>();

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

            while (!cancellationToken.IsCancellationRequested && opts.Duration.TotalMilliseconds > sw.Elapsed.TotalMilliseconds)
            {
                try
                {
                    await sem.WaitAsync();
                    var uri = stream.Take(cancellationToken);
                    bag.Add(job.DoWork(uri).ContinueWith(_ =>
                    {
                        sem.Release();
                    }));
                }
                catch (Exception ex)
                {
                    workerThreadResult.AddError((int)sw.ElapsedMilliseconds / 1000, 0, 0, false, ex);
                }
            }

            await Task.WhenAll(bag);

            results.Enqueue(job.GetResults());
            resetEvent.Set();
        }

        private CombinedWorkerThreadResult QueueCountThreads(CountOptions opts, CancellationToken cancellationToken)
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
                    if (num >= opts.Count)
                    {
                        bc.CompleteAdding();
                        break;
                    }
                    await Task.Delay(_uris.Frequency(opts));
                }
            });

            for (var i = 0; i < opts.Threads; i++)
            {
                var resetEvent = new ManualResetEventSlim(false);

                var thread = new Thread(async (index) => await DoWork_Count(bc, opts.Concurrency, resetEvent, results, (int)index, cancellationToken));

                thread.Start(i);
                events.Add(resetEvent);
            }

            return Sink(results, events, sw);
        }

        private async Task DoWork_Count(BlockingCollection<Uri> stream, int concurrency, ManualResetEventSlim resetEvent, ConcurrentQueue<WorkerThreadResult> results, int workerIndex, CancellationToken cancellationToken)
        {
            var bag = new ConcurrentBag<Task>();
            var sem = new SemaphoreSlim(concurrency);
            var workerThreadResult = new WorkerThreadResult();
            var job = await _workerJob.Init(workerIndex, workerThreadResult);

            while (!stream.IsCompleted && stream.TryTake(out var uri, -1, cancellationToken))
            {
                await sem.WaitAsync();
                bag.Add(job.DoWork(uri).ContinueWith(_ =>
                {
                    sem.Release();
                }));
            }

            await Task.WhenAll(bag);

            results.Enqueue(job.GetResults());
            resetEvent.Set();
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
    }
}