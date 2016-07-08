using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Netling.Core.Models;
using Netling.Core.Performance;

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
                ThreadPool.QueueUserWorkItem((state) =>
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
                                else { 
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
                    }, i);

                events.Add(resetEvent);
            }

            for (var i = 0; i < events.Count; i += 50)
            {
                var group = events.Skip(i).Take(50).Select(r => r.WaitHandle).ToArray();
                WaitHandle.WaitAll(group);
            }
            
            return JobResult.Merge(totalRuntime, results.ToList());
        }
    }
}