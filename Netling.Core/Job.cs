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
    public class Job<T> where T : IResult
    {
        public delegate void ProgressEventHandler(double value);
        public ProgressEventHandler OnProgress { get; set; }

        public JobResult<T> Process(int threads, TimeSpan duration, Func<int, IEnumerable<Task<T>>> processAction, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Process(threads, int.MaxValue, duration, processAction, cancellationToken);
        }

        public JobResult<T> Process(int threads, int runs, Func<int, IEnumerable<Task<T>>> processAction, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Process(threads, runs, TimeSpan.MaxValue, processAction, cancellationToken);
        }

        private JobResult<T> Process(int threads, int runs, TimeSpan duration, Func<int, IEnumerable<Task<T>>> processAction, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThreadPool.SetMinThreads(int.MaxValue, int.MaxValue);

            var results = new ConcurrentQueue<List<T>>();
            var events = new List<ManualResetEvent>();
            var sw = new Stopwatch();
            sw.Start();
            var totalRuntime = 0.0;

            for (int i = 0; i < threads; i++)
            {
                var resetEvent = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(async (state) =>
                    {
                        var index = (int)state;
                        var result = new List<T>();

                        for (int j = 0; j < runs; j++)
                        {
                            foreach (var actionResult in processAction.Invoke(index))
                            {
                                var tmp = await actionResult;

                                if (cancellationToken.IsCancellationRequested || duration.TotalMilliseconds < sw.ElapsedMilliseconds)
                                {
                                    results.Enqueue(result);
                                    resetEvent.Set();
                                    return;
                                }

                                result.Add(tmp);
                                totalRuntime = sw.Elapsed.TotalMilliseconds;

                                if (index == 0 && j % 1000 == 0 && OnProgress != null)
                                {
                                    if (duration == TimeSpan.MaxValue)
                                        OnProgress(100.0/runs*(j + 1));
                                    else
                                        OnProgress(100.0 / duration.TotalMilliseconds * sw.ElapsedMilliseconds);
                                }
                            }
                        }

                        results.Enqueue(result);
                        resetEvent.Set();
                    }, i);

                events.Add(resetEvent);
            }

            for (int i = 0; i < events.Count; i += 50)
            {
                var group = events.Skip(i).Take(50).ToArray();
                WaitHandle.WaitAll(group);
            }

            var finalResults = results.SelectMany(r => r, (a, b) => b).ToList();
            return new JobResult<T>(threads, totalRuntime, finalResults);
        }
    }
}