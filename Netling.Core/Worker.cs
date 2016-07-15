using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Netling.Core.Models;
using Netling.Core.Performance;
using Netling.Core.Utils;

namespace Netling.Core
{
    public static class Worker
    {
        public static Task<WorkerResult> Run(string url, int threads, bool threadAfinity, int pipelining, TimeSpan duration, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var internalWorkerResult = QueueWorkerThreads(url, threads, threadAfinity, pipelining, duration, cancellationToken);
                var workerResult = new WorkerResult(url, threads, threadAfinity, pipelining, internalWorkerResult.Elapsed);
                workerResult.Process(internalWorkerResult);
                return workerResult;
            });
        }

        private static WorkerThreadResult QueueWorkerThreads(string url, int threads, bool threadAfinity, int pipelining, TimeSpan duration, CancellationToken cancellationToken)
        {
            var results = new ConcurrentQueue<WorkerThreadResult>();
            var events = new List<ManualResetEventSlim>();
            var sw = new Stopwatch();
            sw.Start();

            for (var i = 0; i < threads; i++)
            {
                var resetEvent = new ManualResetEventSlim(false);

                ThreadHelper.QueueThread(i, threadAfinity, () =>
                {
                    DoWork(url, duration, pipelining, results, sw, cancellationToken, resetEvent);
                });

                events.Add(resetEvent);
            }

            for (var i = 0; i < events.Count; i += 50)
            {
                var group = events.Skip(i).Take(50).Select(r => r.WaitHandle).ToArray();
                WaitHandle.WaitAll(group);
            }
            sw.Stop();

            return WorkerThreadResult.MergeResults(results.ToList(), sw.Elapsed);
        }

        private static void DoWork(string url, TimeSpan duration, int pipelining, ConcurrentQueue<WorkerThreadResult> results, Stopwatch sw, CancellationToken cancellationToken, ManualResetEventSlim resetEvent)
        {
            var result = new WorkerThreadResult();
            var sw2 = new Stopwatch();
            var worker = new HttpWorker(url);

            // Priming connection ...
            try
            {
                worker.Write();
                worker.Flush();
                worker.Read();
            }
            catch (Exception) { }

            while (!cancellationToken.IsCancellationRequested && duration.TotalMilliseconds > sw.Elapsed.TotalMilliseconds)
            {
                try
                {
                    sw2.Restart();
                    for (var j = 0; j < pipelining; j++)
                    {
                        worker.Write();
                    }

                    worker.Flush();

                    if (pipelining == 1)
                    {
                        var length = worker.Read();
                        result.Add((int)Math.Floor(sw.Elapsed.TotalSeconds), length, (double)sw2.ElapsedTicks / Stopwatch.Frequency * 1000);
                    }
                    else
                    {
                        for (var j = 0; j < pipelining; j++)
                        {
                            var length = worker.ReadPipelined();
                            result.Add((int)Math.Floor(sw.Elapsed.TotalSeconds), length, (double)sw2.ElapsedTicks / Stopwatch.Frequency * 1000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.AddError((int)Math.Floor(sw.Elapsed.TotalSeconds), (double)sw2.ElapsedTicks / Stopwatch.Frequency * 1000);
                }
            }

            results.Enqueue(result);
            resetEvent.Set();
        }
    }
}
