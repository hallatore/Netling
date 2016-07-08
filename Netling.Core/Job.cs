using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Netling.Core.Models;

namespace Netling.Core
{
    public class Job : IJob
    {
        public JobResult Process(int threads, int pipelining, TimeSpan duration, string url, CancellationToken cancellationToken)
        {
            ThreadPool.SetMinThreads(int.MaxValue, int.MaxValue);

            var results = new ConcurrentQueue<JobResult>();
            var events = new List<ManualResetEvent>();
            var sw = new Stopwatch();
            sw.Start();
            var totalRuntime = 0.0;

            for (var i = 0; i < threads; i++)
            {
                var resetEvent = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(async (state) =>
                    {
                        var result = new JobResult();
                        var sw2 = new Stopwatch();

                        while (!cancellationToken.IsCancellationRequested && duration.TotalMilliseconds > sw.ElapsedMilliseconds)
                        {
                            sw2.Restart();

                            try
                            {
                                var request = WebRequest.CreateHttp(url);
                                request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip,deflate,sdch";

                                using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                                using (var stream = response.GetResponseStream())
                                using (var ms = new MemoryStream())
                                {
                                    await stream.CopyToAsync(ms).ConfigureAwait(false);
                                    sw2.Stop();
                                    result.Add((int)Math.Floor(sw.Elapsed.TotalSeconds), ms.Length, (double)sw2.ElapsedTicks / Stopwatch.Frequency * 1000);
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
                var group = events.Skip(i).Take(50).ToArray();
                WaitHandle.WaitAll(group);
            }

            return JobResult.Merge(totalRuntime, results.ToList());
        }
    }
}