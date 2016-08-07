using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Netling.Core.Exceptions;

namespace Netling.Core.Performance
{
    internal class HttpWorker : IDisposable
    {
        private readonly Uri _uri;
        private readonly IPEndPoint _endPoint;
        private readonly byte[] _request;
        private TcpClient _client;
        private Stream _stream;
        private readonly byte[] _buffer;
        private int _bufferIndex;
        private int _read;
        private ResponseType _responseType;
        private byte[] _requestPipelining = null;

        public HttpWorker(Uri uri, HttpMethod httpMethod = HttpMethod.Get, Dictionary<string, string> headers = null, byte[] data = null)
        {
            _buffer = new byte[8192];
            _bufferIndex = 0;
            _read = 0;
            _responseType = ResponseType.Unknown;
            _uri = uri;
            IPAddress ip;
            var headersString = string.Empty;
            var contentLength = data != null ? data.Length : 0;

            if (headers != null && headers.Any())
                headersString = string.Concat(headers.Select(h => "\r\n" + h.Key.Trim() + ": " + h.Value.Trim()));

            if (_uri.HostNameType == UriHostNameType.Dns)
            {
                var host = Dns.GetHostEntry(_uri.Host);
                ip = host.AddressList.First(i => i.AddressFamily == AddressFamily.InterNetwork);
            }
            else
            {
                ip = IPAddress.Parse(_uri.Host);
            }
            
            _endPoint = new IPEndPoint(ip, _uri.Port);
            _request = Encoding.UTF8.GetBytes($"{httpMethod.ToString().ToUpper()} {_uri.PathAndQuery} HTTP/1.1\r\nAccept-Encoding: gzip, deflate, sdch\r\nHost: {_uri.Host}\r\nContent-Length: {contentLength}{headersString}\r\n\r\n");

            if (data == null)
                return;

            var tmpRequest = new byte[_request.Length + data.Length];
            Buffer.BlockCopy(_request, 0, tmpRequest, 0, _request.Length);
            Buffer.BlockCopy(data, 0, tmpRequest, _request.Length, data.Length);
            _request = tmpRequest;
        }

        private byte[] GetPipelineBuffer(int count)
        {
            var result = new byte[_request.Length * count];

            for (var i = 0; i < count; i++)
            {
                Buffer.BlockCopy(_request, 0, result, _request.Length * i, _request.Length);
            }

            return result;
        }

        public void WritePipelined(int pipelining)
        {
            if (_requestPipelining == null)
            {
                _requestPipelining = GetPipelineBuffer(pipelining);
            }

            InitClient();
            _stream.Write(_requestPipelining, 0, _requestPipelining.Length);
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

        public int Read(out int statusCode)
        {
            var read = _stream.Read(_buffer, 0, _buffer.Length);
            var length = read;
            statusCode = HttpHelper.GetStatusCode(_buffer, 0, read);
            _responseType = HttpHelper.GetResponseType(_buffer, 0, read);

            if (_responseType == ResponseType.ContentLength)
            {
                var responseLength = HttpHelperContentLength.GetResponseLength(_buffer, 0, read);

                while (length < responseLength)
                {
                    length += _stream.Read(_buffer, 0, _buffer.Length);
                }
            }
            else if (_responseType == ResponseType.Chunked)
            {
                while (!HttpHelperChunked.IsEndOfChunkedStream(_buffer, read))
                {
                    read = _stream.Read(_buffer, 0, _buffer.Length);
                    length += read;
                }
            }
            else
            {
                throw new UnknownResponseTypeException();
            }

            return length;
        }

        public int ReadPipelined(out int statusCode)
        {
            if (_bufferIndex == 0)
            {
                _read = _stream.Read(_buffer, 0, _buffer.Length);
            }

            _responseType = HttpHelper.GetResponseType(_buffer, _bufferIndex, _read);

            while (_responseType == ResponseType.Unknown)
            {
                // Shift the buffer if we are running out of space
                if (_bufferIndex > _buffer.Length / 2)
                {
                    Buffer.BlockCopy(_buffer, _bufferIndex, _buffer, 0, _read - _bufferIndex);
                    _read -= _bufferIndex;
                    _bufferIndex = 0;
                }

                _read += _stream.Read(_buffer, _read, _buffer.Length - _read);
                _responseType = HttpHelper.GetResponseType(_buffer, _bufferIndex, _read);
            }

            statusCode = HttpHelper.GetStatusCode(_buffer, _bufferIndex, _read);

            if (_responseType == ResponseType.ContentLength)
            {
                var length = _read - _bufferIndex;
                var responseLength = HttpHelperContentLength.GetResponseLength(_buffer, _bufferIndex, _read);

                while (responseLength < 0)
                {
                    // Shift the buffer if we are running out of space
                    if (_bufferIndex > _buffer.Length / 2)
                    {
                        Buffer.BlockCopy(_buffer, _bufferIndex, _buffer, 0, _read - _bufferIndex);
                        _read -= _bufferIndex;
                        _bufferIndex = 0;
                    }

                    _read += _stream.Read(_buffer, _read, _buffer.Length - _read);
                    length = _read - _bufferIndex;
                    responseLength = HttpHelperContentLength.GetResponseLength(_buffer, _bufferIndex, _read);
                }

                while (length < responseLength)
                {
                    _read = _stream.Read(_buffer, 0, _buffer.Length);
                    length += _read;
                }

                var end = _read - (length - responseLength);
                _bufferIndex = _read > end ? end : 0;
                return responseLength;
            }
            else if (_responseType == ResponseType.Chunked)
            {
                var length = 0;
                var streamEnd = HttpHelperChunked.SeekEndOfChunkedStream(_buffer, _bufferIndex, _read);

                if (streamEnd >= 0)
                    length = streamEnd - _bufferIndex;

                while (streamEnd < 0)
                {
                    _read = _stream.Read(_buffer, 0, _buffer.Length);
                    length += _read;
                    streamEnd = HttpHelperChunked.SeekEndOfChunkedStream(_buffer, 0, _read);

                    if (streamEnd >= 0)
                        length += streamEnd;
                }

                _bufferIndex = _read > streamEnd ? streamEnd : 0;
                return length;
            }
            else
            {
                throw new UnknownResponseTypeException();
            }
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
            
            _client.NoDelay = true;
            _client.SendTimeout = 10000;
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
