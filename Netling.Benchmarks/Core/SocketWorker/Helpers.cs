using BenchmarkDotNet.Attributes;
using Netling.Core.SocketWorker.Performance;
using System;
using System.Text;

namespace Netling.Benchmarks.Core.SocketWorker
{
    [MemoryDiagnoser]
    public class Helpers
    {
        private readonly ReadOnlyMemory<byte> _responseContentLength;
        private readonly ReadOnlyMemory<byte> _responseChunked;

        public Helpers()
        {
            _responseContentLength = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nDate: Wed, 06 Jul 2016 18:26:27 GMT\r\nContent-Length: 13\r\nContent-Type: text/plain\r\nServer: Kestrel\r\n\r\nHello, World!").AsMemory();
            _responseChunked = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nDate: Wed, 06 Jul 2016 18:26:27 GMT\r\nTransfer-Encoding: chunked\r\nContent-Type: text/plain\r\nServer: Kestrel\r\n\r\nHello, World!0\r\n\r\n").AsMemory();
        }

        [Benchmark]
        public void SeekHeader() => HttpHelper.SeekHeader(_responseContentLength.Span, CommonStrings.HeaderContentLength.Span, out _, out _);

        [Benchmark]
        public void GetResponseType_ContentLength() => HttpHelper.GetResponseType(_responseContentLength.Span);

        [Benchmark]
        public void GetResponseType_Chunked() => HttpHelper.GetResponseType(_responseChunked.Span);

        [Benchmark]
        public void GetStatusCode() => HttpHelper.GetStatusCode(_responseContentLength.Span);

        [Benchmark]
        public void GetResponseLength() => HttpHelperContentLength.GetResponseLength(_responseContentLength.Span);

        [Benchmark]
        public void GetHeaderContentLength() => HttpHelperContentLength.GetHeaderContentLength(_responseContentLength.Span);

        [Benchmark]
        public void IsEndOfChunkedStream() => HttpHelperChunked.IsEndOfChunkedStream(_responseChunked.Span);

        [Benchmark]
        public void SeekEndOfChunkedStream() => HttpHelperChunked.SeekEndOfChunkedStream(_responseChunked.Span);
    }
}
