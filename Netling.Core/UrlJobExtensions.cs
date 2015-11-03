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

            for (int i = 0; i < threads; i++)
            {
                var client = new WebClient();
                client.Headers[HttpRequestHeader.AcceptEncoding] = "gzip,deflate,sdch";
                WebClients.Add(i, client);
            }

            var result = job.Process(threads, runs, (i) => Action(urls, i), cancellationToken);

            foreach (var client in WebClients.Values)
                client.Dispose();

            WebClients.Clear();
            return result;
        }

        public static JobResult<UrlResult> ProcessUrls(this Job<UrlResult> job, int threads, TimeSpan duration, IEnumerable<string> urls, CancellationToken cancellationToken = default(CancellationToken))
        {
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;

            for (int i = 0; i < threads; i++)
            {
                var client = new WebClient();
                client.Headers[HttpRequestHeader.AcceptEncoding] = "gzip,deflate,sdch";
                WebClients.Add(i, client);
            }

            var result = job.Process(threads, duration, (i) => Action(urls, i), cancellationToken);

            foreach (var client in WebClients.Values)
                client.Dispose();

            WebClients.Clear();
            return result;
        }

        private static Dictionary<int, WebClient> WebClients = new Dictionary<int, WebClient>();

        private static IEnumerable<Task<UrlResult>> Action(IEnumerable<string> urls, int index)
        {
            var client = WebClients[index];
            return urls.Select((u) => { return GetResult(u, client); });
        }

        private static Task<UrlResult> GetResult(string url, WebClient client)
        {
            var startTime = DateTime.Now;

            try
            {
                var sw = new Stopwatch();
                sw.Start();
                var bytes = client.DownloadData(url);
                return Task.FromResult(new UrlResult((double)sw.ElapsedTicks / Stopwatch.Frequency * 1000, bytes.Length, startTime, url, Thread.CurrentThread.ManagedThreadId));
            }
            catch (Exception)
            {
                return Task.FromResult(new UrlResult(startTime, url));
            }
        }
    }
}
