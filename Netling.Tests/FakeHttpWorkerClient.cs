using System;
using System.Text;
using Netling.Core.SocketWorker.Performance;

namespace Netling.Tests
{
    public class FakeHttpWorkerClient : IHttpWorkerClient
    {
        private readonly string[] _results;
        private int _index;

        public FakeHttpWorkerClient(params string[] results)
        {
            _results = results;
            _index = 0;
        }

        public void Dispose()
        {
            
        }

        public int Read(Memory<byte> buffer)
        {
            var source = Encoding.UTF8.GetBytes(_results[_index++]).AsSpan();
            source.CopyTo(buffer.Span);
            return source.Length;
        }
        
        public void Write(ReadOnlySpan<byte> buffer)
        {
            _index = 0;
        }
    }
}
