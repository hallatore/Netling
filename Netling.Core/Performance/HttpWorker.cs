using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Netling.Core.Performance
{
    public class HttpWorker : IDisposable
    {
        private readonly Uri _uri;
        private readonly IPEndPoint _endPoint;
        private readonly byte[] _request;
        private TcpClient _client;
        private ResponseType _responseType;

        public HttpWorker(string url)
        {
            _responseType = ResponseType.Unknown;
            _uri = new Uri(url, UriKind.Absolute);
            var host = Dns.GetHostEntry(_uri.Host);
            var ip = host.AddressList.First(i => i.AddressFamily == AddressFamily.InterNetwork);
            _endPoint = new IPEndPoint(ip, _uri.Port);
            _request = Encoding.UTF8.GetBytes($"GET {_uri.PathAndQuery} HTTP/1.1\r\nAccept-Encoding: gzip, deflate, sdch\r\nConnection: keep-alive\r\nHost: {_uri.Host}\r\nContent-Length: 0\r\n\r\n");
        }

        public int Get()
        {
            InitClient();
            var stream = HttpHelper.GetStream(_client, _uri);
            stream.Write(_request, 0, _request.Length);
            stream.Flush();
            return GetLength();
        }

        private int GetLength()
        {
            const int bufferSize = 4096;
            var buffer = new byte[bufferSize];
            var length = 0;

            var read = _client.Client.Receive(buffer);
            length += read;

            if (_responseType == ResponseType.Unknown)
                _responseType = HttpHelper.GetResponseType(buffer);

            if (_responseType == ResponseType.Chunked && !HttpHelperChunked.IsEndOfChunkedStream(read, buffer))
            {
                length += HttpHelperChunked.ReadChunkedStream(_client);
            }
            else if (_responseType == ResponseType.ContentLength)
            {
                var totalLength = HttpHelper.SeekDoubleReturn(buffer, 0) + 4 + HttpHelperContentLength.GetContentLength(buffer);

                if (totalLength > length)
                    length = HttpHelperContentLength.ReadContentLengthStream(_client, read, totalLength);
            }

            return length;
        }

        private void InitClient()
        {
            if (_client != null && _client.Connected)
                return;

            _client?.Close();
            _client = new TcpClient();

            const int sioLoopbackFastPath = -1744830448;
            var optionInValue = BitConverter.GetBytes(1);

            try
            {
                _client.Client.IOControl(sioLoopbackFastPath, optionInValue, null);
            }
            catch (SocketException) { }

            _client.ReceiveTimeout = 10000;
            _client.Connect(_endPoint);
        }

        public void Dispose()
        {
            _client?.Close();
        }
    }
}
