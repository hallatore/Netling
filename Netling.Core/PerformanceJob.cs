using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Netling.Core.Models;
using Netling.Core.Performance;
using System.Runtime.InteropServices;

namespace Netling.Core
{
    public class PerformanceJob : IJob
    {
        public JobResult Process(int threads, int pipelining, TimeSpan duration, string url, CancellationToken cancellationToken)
        {
            ThreadPool.SetMinThreads(int.MaxValue, int.MaxValue);
            var results = new ConcurrentQueue<JobResult>();
            var events = new List<ManualResetEventSlim>();
            var sw = new Stopwatch();
            sw.Start();
            var totalRuntime = 0.0;

            for (var i = 0; i < threads; i++)
            {
                var resetEvent = new ManualResetEventSlim(false);
                QueueThreadWithAfinity(i, () =>
                {
                    var result = new JobResult();
                    var sw2 = new Stopwatch();
                    var worker = new HttpWorker(url);

                    while (!cancellationToken.IsCancellationRequested && duration.TotalMilliseconds > sw.ElapsedMilliseconds)
                    {
                        sw2.Restart();

                        try
                        {
                            if (pipelining == 1)
                            {
                                worker.Write();
                                worker.Flush();
                                var length = worker.Read();
                                result.Add((int)Math.Floor(sw.Elapsed.TotalSeconds), length, (double)sw2.ElapsedTicks / Stopwatch.Frequency * 1000);
                            }
                            else
                            {
                                for (var j = 0; j < pipelining; j++)
                                {
                                    worker.Write();
                                }

                                worker.Flush();

                                for (var j = 0; j < pipelining; j++)
                                {
                                    var length = worker.ReadPipelined();
                                    result.Add((int)Math.Floor(sw.Elapsed.TotalSeconds), length, (double)sw2.ElapsedTicks / Stopwatch.Frequency * 1000);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            result.AddError((int)Math.Floor(sw.Elapsed.TotalSeconds));
                        }
                    }

                    results.Enqueue(result);
                    resetEvent.Set();
                    totalRuntime = sw.Elapsed.TotalMilliseconds;
                });

                events.Add(resetEvent);
            }

            for (var i = 0; i < events.Count; i += 50)
            {
                var group = events.Skip(i).Take(50).Select(r => r.WaitHandle).ToArray();
                WaitHandle.WaitAll(group);
            }
            
            return JobResult.Merge(totalRuntime, results.ToList());
        }

        private void QueueThread(int i, Action action)
        {
            ThreadPool.QueueUserWorkItem((s) => {
                action.Invoke();
            });
        }

        private void QueueThreadWithAfinity(int i, Action action)
        {
            var thread = new Thread(() => {
                Thread.BeginThreadAffinity();
                var afinity = GetAfinity(i + 1, Environment.ProcessorCount);
                CurrentThread.ProcessorAffinity = new IntPtr(1 << afinity);
                action.Invoke();
                Thread.EndThreadAffinity();
            });
            thread.Start();
        }

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        private ProcessThread CurrentThread
        {
            get
            {
                int id = GetCurrentThreadId();
                return
                    (from ProcessThread th in System.Diagnostics.Process.GetCurrentProcess().Threads
                     where th.Id == id
                     select th).Single();
            }
        }

        private static int GetAfinity(int i, int cores)
        {
            var afinity = i * 2 % cores;

            if (i % cores >= cores / 2)
                afinity++;

            return afinity;
        }
    }
}