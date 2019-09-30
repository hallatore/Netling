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
        private readonly Uri _uri;
        private readonly Stopwatch _stopwatch;
        private readonly Stopwatch _localStopwatch;
        private readonly WorkerThreadResult _workerThreadResult;
        private readonly HttpClient _httpClient;
        private readonly IEnumerable<Uri> _uris;

        // Used to approximately calculate bandwidth
        private static readonly int MissingHeaderLength = "HTTP/1.1 200 OK\r\nContent-Length: 123\r\nContent-Type: text/plain\r\n\r\n".Length;

        public HttpClientWorkerJob(IEnumerable<Uri> uris)
        {
            _uris = uris;
        }

        private HttpClientWorkerJob(int index, IEnumerable<Uri> uris, WorkerThreadResult workerThreadResult)
        {
            _index = index;
            _uris = uris;
            _stopwatch = Stopwatch.StartNew();
            _localStopwatch = new Stopwatch();
            _workerThreadResult = workerThreadResult;
            _httpClient = new HttpClient();
        }

        public HttpClientWorkerJob(Uri uri)
        {
            _uri = uri;
        }

        private HttpClientWorkerJob(int index, Uri uri, WorkerThreadResult workerThreadResult)
        {
            _index = index;
            _uri = uri;
            _stopwatch = Stopwatch.StartNew();
            _localStopwatch = new Stopwatch();
            _workerThreadResult = workerThreadResult;
            _httpClient = new HttpClient();
        }

        public async ValueTask DoWork()
        {
            _localStopwatch.Restart();

            using (var response = await _httpClient.GetAsync(_uri))
            {
                var contentStream = await response.Content.ReadAsStreamAsync();
                var length = contentStream.Length + response.Headers.ToString().Length + MissingHeaderLength;
                var responseTime = (float)_localStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
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
            return new ValueTask<IWorkerJob>(new HttpClientWorkerJob(index, _uri, workerThreadResult));
        }
    }
}
