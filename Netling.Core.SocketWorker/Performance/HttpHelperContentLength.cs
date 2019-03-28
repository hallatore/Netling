using System;
using Netling.Core.SocketWorker.Extensions;

namespace Netling.Core.SocketWorker.Performance
{
    public static class HttpHelperContentLength
    {
        public static int GetHeaderContentLength(ReadOnlySpan<byte> buffer)
        {
            HttpHelper.SeekHeader(buffer, CommonStrings.HeaderContentLength.Span, out var index, out var length);
            return ByteExtensions.ConvertToInt(buffer.Slice(index, length));
        }
        
        public static int GetResponseLength(ReadOnlySpan<byte> buffer)
        {
            if (!HttpHelper.SeekHeader(buffer, CommonStrings.HeaderContentLength.Span, out var index, out var length))
            {
                return -1;
            }

            var headerEndIndex = buffer.Slice(index + length).IndexOf(CommonStrings.HeaderEnd.Span);

            if (headerEndIndex < 0)
            {
                return -1;
            }

            var contentLength = ByteExtensions.ConvertToInt(buffer.Slice(index, length));
            return index + length + headerEndIndex + 4 + contentLength;
        }
    }
}
