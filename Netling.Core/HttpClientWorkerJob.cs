using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Netling.Core.Models;

namespace Netling.Core
{
    public class HttpClientWorkerJob : IWorkerJob
    {
        private readonly int _index;
        private readonly Stopwatch _stopwatch;
        private readonly WorkerThreadResult _workerThreadResult;
        private readonly HttpClient _httpClient;

        // Used to approximately calculate bandwidth
        private static readonly int MissingHeaderLength = "HTTP/1.1 200 OK\r\nContent-Length: 123\r\nContent-Type: text/plain\r\n\r\n".Length;

        public HttpClientWorkerJob()
        {
        }

        private HttpClientWorkerJob(int index, WorkerThreadResult workerThreadResult)
        {
            _index = index;
            _stopwatch = Stopwatch.StartNew();
            _workerThreadResult = workerThreadResult;
            _httpClient = new HttpClient();
        }

        public async Task DoWork(Uri uri)
        {
            var localStopwatch = Stopwatch.StartNew();

            using (var response = await _httpClient.GetAsync(uri))
            {
                var contentStream = await response.Content.ReadAsStreamAsync();
                var length = contentStream.Length + response.Headers.ToString().Length + MissingHeaderLength;
                var responseTime = (float)localStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
                var statusCode = (int)response.StatusCode;

                if (statusCode < 400 || statusCode == 404) // not found is a valid response
                {
                    _workerThreadResult.Add((int)_stopwatch.ElapsedMilliseconds / 1000, length, responseTime, statusCode, _index < 10);
                }
                else
                {
                    _workerThreadResult.AddError((int)_stopwatch.ElapsedMilliseconds / 1000, responseTime, statusCode, _index < 10);
                }
            }
        }

        public WorkerThreadResult GetResults()
        {
            return _workerThreadResult;
        }

        public ValueTask<IWorkerJob> Init(int index, WorkerThreadResult workerThreadResult)
        {
            return new ValueTask<IWorkerJob>(new HttpClientWorkerJob(index, workerThreadResult));
        }
    }
}
