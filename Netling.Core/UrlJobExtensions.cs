using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Netling.Core.Models;

namespace Netling.Core
{
    public static class UrlJobExtensions
    {
        public static JobResult<UrlResult> ProcessUrls(this Job<UrlResult> job, int threads, int runs, IEnumerable<string> urls, CancellationToken cancellationToken = default(CancellationToken))
        {
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            return job.Process(threads, runs, () => Action(urls), cancellationToken);
        }

        public static JobResult<UrlResult> ProcessUrls(this Job<UrlResult> job, int threads, TimeSpan duration, IEnumerable<string> urls, CancellationToken cancellationToken = default(CancellationToken))
        {
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            return job.Process(threads, duration, () => Action(urls), cancellationToken);
        }

        private static IEnumerable<Task<UrlResult>> Action(IEnumerable<string> urls)
        {
            return urls.Select(GetResult);
        }

        private static async Task<UrlResult> GetResult(string url)
        {
            var startTime = DateTime.Now;

            try
            {
                var sw = new Stopwatch();
                sw.Start();
                var webRequest = (HttpWebRequest)WebRequest.Create(url);
                webRequest.Headers[HttpRequestHeader.AcceptEncoding] = "gzip,deflate,sdch";

                using (var response = (HttpWebResponse)await webRequest.GetResponseAsync())
                using (var stream = response.GetResponseStream())
                using (var sr = new StreamReader(stream))
                {
                    var result = await sr.ReadToEndAsync();
                    if (response.StatusCode == HttpStatusCode.OK)
                        return new UrlResult((int)sw.ElapsedMilliseconds, result.Length, startTime, url, Thread.CurrentThread.ManagedThreadId);
                    
                    return new UrlResult(startTime, url);
                }
            }
            catch (Exception)
            {
                return new UrlResult(startTime, url);
            }
        }
    }
}
