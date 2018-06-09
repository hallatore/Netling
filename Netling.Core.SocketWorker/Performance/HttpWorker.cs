using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Netling.Core.Exceptions;

namespace Netling.Core.SocketWorker.Performance
{
    public class HttpWorker : IDisposable
    {
        private readonly IHttpWorkerClient _client;
        private readonly byte[] _request;
        private readonly byte[] _buffer;
        private ResponseType _responseType;

        public HttpWorker(IHttpWorkerClient client, Uri uri, HttpMethod httpMethod = HttpMethod.Get, Dictionary<string, string> headers = null, byte[] data = null)
        {
            _client = client;
            _buffer = new byte[8192];
            _responseType = ResponseType.Unknown;
            var headersString = string.Empty;
            var contentLength = data?.Length ?? 0;

            if (headers != null && headers.Any())
                headersString = string.Concat(headers.Select(h => "\r\n" + h.Key.Trim() + ": " + h.Value.Trim()));
            
            _request = Encoding.UTF8.GetBytes($"{httpMethod.ToString().ToUpper()} {uri.PathAndQuery} HTTP/1.1\r\nAccept-Encoding: gzip, deflate, sdch\r\nHost: {uri.Host}\r\nContent-Length: {contentLength}{headersString}\r\n\r\n");

            if (data != null)
            {
                var tmpRequest = new byte[_request.Length + data.Length];
                Buffer.BlockCopy(_request, 0, tmpRequest, 0, _request.Length);
                Buffer.BlockCopy(data, 0, tmpRequest, _request.Length, data.Length);
                _request = tmpRequest;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write()
        {
            _client.Write(_request, 0, _request.Length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Flush()
        {
            _client.Flush();
        }

        public int Read(out int statusCode)
        {
            var read = _client.Read(_buffer, 0, _buffer.Length);
            var length = read;
            statusCode = HttpHelper.GetStatusCode(_buffer, 0, read);
            _responseType = HttpHelper.GetResponseType(_buffer, 0, read);

            if (_responseType == ResponseType.ContentLength)
            {
                var responseLength = HttpHelperContentLength.GetResponseLength(_buffer, 0, read);

                while (length < responseLength)
                {
                    length += _client.Read(_buffer, 0, _buffer.Length);
                }
                
                return length;
            }
            
            if (_responseType == ResponseType.Chunked)
            {
                while (!HttpHelperChunked.IsEndOfChunkedStream(_buffer, read))
                {
                    read = _client.Read(_buffer, 0, _buffer.Length);
                    length += read;
                }

                return length;
            }
            
            throw new UnknownResponseTypeException();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
