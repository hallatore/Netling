using System;
using System.Text;

namespace Netling.Core.SocketWorker.Performance
{
    public static class CommonStrings
    {
        public static readonly ReadOnlyMemory<byte> HeaderContentLength = Encoding.ASCII.GetBytes("\r\nContent-Length: ");
        public static readonly ReadOnlyMemory<byte> HeaderTransferEncoding = Encoding.ASCII.GetBytes("\r\nTransfer-Encoding: ");
        public static readonly ReadOnlyMemory<byte> HeaderReturn = Encoding.ASCII.GetBytes("\r\n");
        public static readonly ReadOnlyMemory<byte> HeaderEnd = Encoding.ASCII.GetBytes("\r\n\r\n");
        public static readonly ReadOnlyMemory<byte> EndOfChunkedResponse = Encoding.ASCII.GetBytes("0\r\n\r\n");
    }
}
