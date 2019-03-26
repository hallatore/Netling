using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Netling.Core.Models;
using Netling.Core.SocketWorker.Performance;

namespace Netling.Core.SocketWorker
{
    public class SocketWorkerJob : IWorkerJob
    {
        private readonly int _index;
        private readonly Uri _uri;
        private readonly Stopwatch _stopwatch;
        private readonly Stopwatch _localStopwatch;
        private readonly WorkerThreadResult _workerThreadResult;
        private readonly HttpWorker _httpWorker;

        public SocketWorkerJob(Uri uri)
        {
            _uri = uri;
        }

        private SocketWorkerJob(int index, Uri uri, WorkerThreadResult workerThreadResult)
        {
            _index = index;
            _uri = uri;
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
            _localStopwatch = new Stopwatch();
            _workerThreadResult = workerThreadResult;

            IPAddress ip;
            
            if (_uri.HostNameType == UriHostNameType.Dns)
            {
                var host = Dns.GetHostEntry(_uri.Host);
                ip = host.AddressList.First(i => i.AddressFamily == AddressFamily.InterNetwork);
            }
            else
            {
                ip = IPAddress.Parse(_uri.Host);
            }
            
            var endPoint = new IPEndPoint(ip, _uri.Port);
            _httpWorker = new HttpWorker(new HttpWorkerClient(endPoint, uri), uri);
        }

        public Task DoWork()
        {
            _localStopwatch.Restart();
            _httpWorker.Write();
            var length = _httpWorker.Read(out var statusCode);

            if (statusCode < 400)
                _workerThreadResult.Add((int)_stopwatch.ElapsedMilliseconds, length, (float) _localStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000, _index < 10);
            else
                _workerThreadResult.AddError((int)_stopwatch.ElapsedMilliseconds, (float) _localStopwatch.ElapsedTicks / Stopwatch.Frequency * 1000, _index < 10);

            return Task.CompletedTask;
        }

        public WorkerThreadResult GetResults()
        {
            return _workerThreadResult;
        }

        public Task<IWorkerJob> Init(int index, WorkerThreadResult workerThreadResult)
        {
            return Task.FromResult<IWorkerJob>(new SocketWorkerJob(index, _uri, workerThreadResult));
        }
    }
}