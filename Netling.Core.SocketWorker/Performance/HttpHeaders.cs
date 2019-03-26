using System;
using System.Text;

namespace Netling.Core.SocketWorker.Performance
{
    public static class HttpHeaders
    {
        public static readonly ReadOnlyMemory<byte> ContentLength = Encoding.ASCII.GetBytes("\r\nContent-Length: ");
        public static readonly ReadOnlyMemory<byte> TransferEncoding = Encoding.ASCII.GetBytes("\r\nTransfer-Encoding: ");
    }
}
