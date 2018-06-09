using System;

namespace Netling.Core.SocketWorker.Performance
{
    public interface IHttpWorkerClient : IDisposable
    {
        int Read(byte[] buffer, int offset, int count);
        void Write(byte[] buffer, int offset, int count);
        void Flush();
    }
}