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

        public JobResult<T> Process(int threads, TimeSpan duration, Func<IEnumerable<Task<T>>> processAction, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Process(threads, int.MaxValue, duration, processAction, cancellationToken);
        }

        public JobResult<T> Process(int threads, int runs, Func<IEnumerable<Task<T>>> processAction, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Process(threads, runs, TimeSpan.MaxValue, processAction, cancellationToken);
        }

        private JobResult<T> Process(int threads, int runs, TimeSpan duration, Func<IEnumerable<Task<T>>> processAction, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThreadPool.SetMinThreads(int.MaxValue, int.MaxValue);

            var results = new ConcurrentQueue<List<T>>();
            var events = new List<ManualResetEvent>();
            var sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < threads; i++)
            {
                var resetEvent = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(async (state) =>
                    {
                        var index = (int)state;
                        var result = new List<T>();

                        for (int j = 0; j < runs; j++)
                        {
                            foreach (var actionResult in processAction.Invoke())
                            {
                                result.Add(await actionResult);

                                if (index == 0 && OnProgress != null)
                                {
                                    if (duration == TimeSpan.MaxValue)
                                        OnProgress(100.0/runs*(j + 1));
                                    else
                                        OnProgress(100.0 / duration.TotalMilliseconds * sw.ElapsedMilliseconds);
                                }

                                if (cancellationToken.IsCancellationRequested || duration.TotalMilliseconds < sw.ElapsedMilliseconds)
                                {
                                    results.Enqueue(result);
                                    resetEvent.Set();
                                    return;
                                }
                            }
                        }

                        results.Enqueue(result);
                        resetEvent.Set();
                    }, i);

                events.Add(resetEvent);
            }

            WaitHandle.WaitAll(events.ToArray());

            return new JobResult<T>(threads, runs, sw.Elapsed.TotalMilliseconds, results.SelectMany(r => r, (a, b) => b).ToList());
        }
    }
}