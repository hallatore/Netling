using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            return job.Process(threads, runs, () => Action(urls), cancellationToken);
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
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                        return new UrlResult((int)sw.ElapsedMilliseconds, response.ContentLength, startTime, url, Thread.CurrentThread.ManagedThreadId);
                    
                    return new UrlResult(startTime, url);
                }
            }
            catch (Exception ex)
            {
                return new UrlResult(startTime, url);
            }
        }
    }
}