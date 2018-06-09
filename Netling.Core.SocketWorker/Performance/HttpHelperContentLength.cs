using System.Runtime.CompilerServices;
using Netling.Core.SocketWorker.Extensions;

namespace Netling.Core.SocketWorker.Performance
{
    public static class HttpHelperContentLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHeaderContentLength(byte[] buffer, int start, int end)
        {
            HttpHelper.SeekHeader(buffer, HttpHeaders.ContentLength, start, end, out var index, out var length);
            return ByteExtensions.ConvertToInt(buffer, index, length, end);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetResponseLength(byte[] buffer, int start, int end)
        {
            var headerEnd = HttpHelper.SeekHeaderEnd(buffer, start, end);

            if (headerEnd < 0)
                return -1;

            var contentLength = GetHeaderContentLength(buffer, start, end);

            return headerEnd - start + 4 + contentLength;
        }
    }
}
