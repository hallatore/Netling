namespace Netling.Core.Performance
{
    public static class HttpHelperContentLength
    {
        public static int GetHeaderContentLength(byte[] buffer, int start, int end)
        {
            int index;
            int length;
            HttpHelper.SeekHeader(buffer, HttpHeaders.ContentLength, start, end, out index, out length);
            return ConvertToInt(buffer, index, length);
        }

        private static int ConvertToInt(byte[] bytes, int start, int length)
        {
            var result = 0;

            for (var i = 0; i < length; i++)
            {
                result = result * 10 + (bytes[start + i] - '0');
            }

            return result;
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
