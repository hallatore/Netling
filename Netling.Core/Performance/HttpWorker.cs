using System;
using System.IO;
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
        private Stream _stream;
        private readonly byte[] _buffer;
        private int _streamIndex;
        private int _read;
        private ResponseType _responseType;

        public HttpWorker(string url)
        {
            _buffer = new byte[4096];
            _streamIndex = 0;
            _read = 0;
            _responseType = ResponseType.Unknown;
            _uri = new Uri(url, UriKind.Absolute);
            var host = Dns.GetHostEntry(_uri.Host);
            var ip = host.AddressList.First(i => i.AddressFamily == AddressFamily.InterNetwork);
            _endPoint = new IPEndPoint(ip, _uri.Port);
            _request = Encoding.UTF8.GetBytes($"GET {_uri.PathAndQuery} HTTP/1.1\r\nAccept-Encoding: gzip, deflate, sdch\r\nConnection: keep-alive\r\nHost: {_uri.Host}\r\nContent-Length: 0\r\n\r\n");
        }

        public void Write()
        {
            InitClient();
            _stream.Write(_request, 0, _request.Length);
        }

        public void Flush()
        {
            _stream.Flush();
        }

        public int Read()
        {
            var read = _client.Client.Receive(_buffer);
            var length = read;
            _responseType = HttpHelper.GetResponseType(_buffer);

            if (_responseType == ResponseType.ContentLength)
            {
                var responseLength = HttpHelper.SeekHeaderEnd(_buffer, 0) + 4 + HttpHelperContentLength.GetContentLength(_buffer, 0);

                while (length < responseLength)
                {
                    length += _client.Client.Receive(_buffer);
                }
            }
            else if (_responseType == ResponseType.Chunked)
            {
                while (!HttpHelperChunked.IsEndOfChunkedStream(_buffer, read))
                {
                    read = _client.Client.Receive(_buffer);
                    length += read;
                }
            }

            return length;
        }

        // experimental and not working yet ...
        public int ReadPipelined()
        {
            if (_streamIndex == 0)
            {
                _read = _client.Client.Receive(_buffer);
                _responseType = HttpHelper.GetResponseType(_buffer);
            }

            var length = _read - _streamIndex;
            var responseLength = HttpHelper.SeekHeaderEnd(_buffer, _streamIndex) - _streamIndex + 4 + HttpHelperContentLength.GetContentLength(_buffer, _streamIndex);

            while (length < responseLength)
            {
                _read = _client.Client.Receive(_buffer);
                length += _read;
            }

            var end = _read - (length - responseLength);

            if (_read > end && end > 0 && HttpHelper.IsHeaderStart(_buffer, end))
            {
                _streamIndex = end;
            }
            else
            {
                _streamIndex = 0;
            }

            return responseLength;
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
            _stream = HttpHelper.GetStream(_client, _uri);
        }

        public void Dispose()
        {
            _client?.Close();
        }
    }
}
