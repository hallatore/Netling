namespace Netling.Core.Performance
{
    public static class HttpHelperContentLength
    {
        public static int GetContentLength(byte[] buffer, int start)
        {
            int index;
            int length;
            HttpHelper.SeekHeader(buffer, HttpHeaders.ContentLength, start, out index, out length);
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
    }
}
