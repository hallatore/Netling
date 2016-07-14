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
        public JobResult Process(int threads, bool threadAfinity, int pipelining, TimeSpan duration, string url, CancellationToken cancellationToken)
        {
            var results = new ConcurrentQueue<JobResult>();
            var events = new List<ManualResetEventSlim>();
            var sw = new Stopwatch();
            sw.Start();

            for (var i = 0; i < threads; i++)
            {
                var resetEvent = new ManualResetEventSlim(false);

                QueueThread(i, threadAfinity, () =>
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
            
            var result = JobResult.Merge(sw.Elapsed.TotalMilliseconds, results.ToList());
            result.Url = url;
            result.Threads = threads;
            result.ThreadAfinity = threadAfinity;
            result.Pipelining = pipelining;
            return result;
        }

        private void DoWork(string url, TimeSpan duration, int pipelining, ConcurrentQueue<JobResult> results, Stopwatch sw, CancellationToken cancellationToken, ManualResetEventSlim resetEvent)
        {
            Debug.WriteLine("Thread created");
            var result = new JobResult();
            var sw2 = new Stopwatch();
            var worker = new HttpWorker(url);

            // Priming connection ...
            try
            {
                worker.Write();
                worker.Flush();
                worker.Read();
            }
            catch (Exception) {}

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

        private void QueueThread(int i, bool useThreadAfinity, Action action)
        {
            var thread = new Thread(() => {
                if (useThreadAfinity)
                {
                    Thread.BeginThreadAffinity();
                    var afinity = GetAfinity(i + 1, Environment.ProcessorCount);
                    CurrentThread.ProcessorAffinity = new IntPtr(1 << afinity);
                }
                
                action.Invoke();

                if (useThreadAfinity)
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
                var id = GetCurrentThreadId();
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