using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Netling.Core.Models;

namespace Netling.Core.HttpClientWorker
{
    public class HttpClientWorkerJob : IWorkerJob
    {
        private readonly int _index;
        private readonly Uri _uri;
        private readonly Stopwatch _stopwatch;
        private readonly Stopwatch _localStopwatch;
        private readonly WorkerThreadResult _workerThreadResult;
        private readonly HttpClient _httpClient;

        // Used to approximately calculate bandwidth
        private static readonly int MissingHeaderLength = "HTTP/1.1 200 OK\r\nContent-Length: 123\r\nContent-Type: text/plain\r\n\r\n".Length; 

        public HttpClientWorkerJob(Uri uri)
        {
            _uri = uri;
        }

        private HttpClientWorkerJob(int index, Uri uri, WorkerThreadResult workerThreadResult)
        {
            _index = index;
            _uri = uri;
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
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

                if ((int)response.StatusCode < 400)
                    _workerThreadResult.Add((int)_stopwatch.ElapsedMilliseconds / 1000, length, responseTime, _index < 10);
                else
                    _workerThreadResult.AddError((int)_stopwatch.ElapsedMilliseconds / 1000, responseTime, _index < 10);
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