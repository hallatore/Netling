using System;

namespace Netling.Core.SocketWorker.Performance
{
    public interface IHttpWorkerClient : IDisposable
    {
        int Read(Memory<byte> buffer);
        void Write(ReadOnlySpan<byte> buffer);
    }
}