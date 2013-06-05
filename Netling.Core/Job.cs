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
        public delegate void ProgressEventHandler(int value);
        public ProgressEventHandler OnProgress { get; set; }

        public JobResult<T> Process(int threads, int runs, Func<IEnumerable<Task<T>>> processAction, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThreadPool.SetMinThreads(threads, threads);

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
                                    OnProgress(result.Count * threads);

                                if (cancellationToken.IsCancellationRequested)
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