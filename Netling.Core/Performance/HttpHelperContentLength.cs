using Netling.Core.Extensions;

namespace Netling.Core.Performance
{
    internal static class HttpHelperContentLength
    {
        public static int GetHeaderContentLength(byte[] buffer, int start, int end)
        {
            int index;
            int length;
            HttpHelper.SeekHeader(buffer, HttpHeaders.ContentLength, start, end, out index, out length);
            return buffer.ConvertToInt(index, length, end);
        }

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
