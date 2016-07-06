using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Netling.Core.Models;

namespace Netling.Core
{
    public class Job
    {
        public async Task<JobResult> Process(int threads, TimeSpan duration, string url, int pipelining, CancellationToken cancellationToken)
        {
            ThreadPool.SetMinThreads(int.MaxValue, int.MaxValue);

            var results = new Queue<List<UrlResult>>(threads);
            var requests = new List<Task>(threads);
            var sw = new Stopwatch();
            sw.Start();
            var totalRuntime = 0.0;

            for (var i = 0; i < threads; i++)
            {
                var result = new List<UrlResult>();
                results.Enqueue(result);

                var request = SubmitRequests(duration, url, pipelining, sw, result, cancellationToken);
                requests.Add(request);

            }

            await Task.WhenAll(requests.ToArray()).ConfigureAwait(false);

            totalRuntime = sw.Elapsed.TotalMilliseconds;

            var finalResults = results.SelectMany(r => r, (a, b) => b).ToList();
            return new JobResult(threads, totalRuntime, finalResults);
        }

        private static async Task SubmitRequests(TimeSpan duration, string url, int pipelining, Stopwatch sw, List<UrlResult> result, CancellationToken cancellationToken)
        {
            var sw2 = new Stopwatch();
            var tasks = new List<Task<UrlResult>>(pipelining);

            var webRequestHandler = new WebRequestHandler();
            webRequestHandler.UseDefaultCredentials = true;
            webRequestHandler.AllowPipelining = true;

            using (var client = new HttpClient(webRequestHandler))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip,deflate,sdch");

                while (!cancellationToken.IsCancellationRequested && duration.TotalMilliseconds > sw.ElapsedMilliseconds)
                {
                    tasks.Clear();
                    sw2.Restart();

                    for (var i = 0; i < pipelining; i++)
                    {
                        tasks.Add(SubmitRequests(url, sw, result, sw2, client));
                    }

                    await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);

                    foreach (var task in tasks)
                    {
                        result.Add(task.Result);
                    }
                }
            }
        }

        private static async Task<UrlResult> SubmitRequests(string url, Stopwatch sw, List<UrlResult> result, Stopwatch sw2, HttpClient client)
        {
            try
            {
                using (var response = await client.GetAsync(url).ConfigureAwait(false))
                {
                    var elapsed = sw.Elapsed;
                    return new UrlResult(elapsed.TotalMilliseconds, (double)sw2.ElapsedTicks / Stopwatch.Frequency * 1000, response.Content.Headers.ContentLength.Value);
                }
            }
            catch (Exception)
            {
               return new UrlResult(sw.Elapsed.TotalMilliseconds);
            }
        }
    }
}