using System.Text;

namespace Netling.Core.Performance
{
    internal static class HttpHeaders
    {
        public static readonly byte[] ContentLength = Encoding.ASCII.GetBytes("\r\nContent-Length: ");
        public static readonly byte[] TransferEncoding = Encoding.ASCII.GetBytes("\r\nTransfer-Encoding: ");
    }
}
