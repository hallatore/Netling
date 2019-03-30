using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netling.Core.Exceptions;

namespace Netling.Core.SocketWorker.Performance
{
    public class HttpWorker : IDisposable
    {
        private readonly IHttpWorkerClient _client;
        private readonly ReadOnlyMemory<byte> _request;
        private readonly Memory<byte> _buffer;

        public HttpWorker(IHttpWorkerClient client, Uri uri, HttpMethod httpMethod = HttpMethod.Get, Dictionary<string, string> headers = null, byte[] data = null)
        {
            _client = client;
            _buffer = new Memory<byte>(new byte[8192]);
            var headersString = string.Empty;
            var contentLength = data?.Length ?? 0;

            if (headers != null && headers.Any())
            {
                headersString = string.Concat(headers.Select(h => "\r\n" + h.Key.Trim() + ": " + h.Value.Trim()));
            }

            var request = Encoding.UTF8.GetBytes($"{httpMethod.ToString().ToUpper()} {uri.PathAndQuery} HTTP/1.1\r\nAccept-Encoding: gzip, deflate, sdch\r\nHost: {uri.Host}\r\nContent-Length: {contentLength}{headersString}\r\n\r\n");

            if (data != null)
            {
                var tmpRequest = new byte[request.Length + data.Length];
                Buffer.BlockCopy(request, 0, tmpRequest, 0, request.Length);
                Buffer.BlockCopy(data, 0, tmpRequest, request.Length, data.Length);
                request = tmpRequest;
            }

            _request = request.AsMemory();
        }

        public (int length, int statusCode) Send()
        {
            _client.Write(_request.Span);
            var read = _client.Read(_buffer);

            if (read == 0)
            {
                _client.Reset();
                throw new ConnectionClosedException();
            }

            var length = read;
            var responseSpan = _buffer.Span.Slice(0, read);
            var statusCode = HttpHelper.GetStatusCode(responseSpan);
            var responseType = HttpHelper.GetResponseType(responseSpan);

            if (responseType == ResponseType.ContentLength)
            {
                var responseLength = HttpHelperContentLength.GetResponseLength(responseSpan);

                while (length < responseLength)
                {
                    length += _client.Read(_buffer);
                }
                
                return (length, statusCode);
            }
            
            if (responseType == ResponseType.Chunked)
            {
                while (!HttpHelperChunked.IsEndOfChunkedStream(_buffer.Span.Slice(0, read)))
                {
                    read = _client.Read(_buffer);
                    length += read;
                }

                return (length, statusCode);
            }
            
            throw new UnknownResponseTypeException();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
