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

        public void Flush()
        {
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            var source = Encoding.UTF8.GetBytes(_results[_index++]);
            var length = Math.Min(count, source.Length);
            Buffer.BlockCopy(source, 0, buffer, offset, length);
            return length;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _index = 0;
        }
    }
}
