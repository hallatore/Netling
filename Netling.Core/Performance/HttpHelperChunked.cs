namespace Netling.Core.Performance
{
    internal static class HttpHelperChunked
    {
        public static bool IsEndOfChunkedStream(byte[] buffer, int end)
        {
            if (end < 7)
                return true;

            return buffer[end - 7] == 13 &&
                   buffer[end - 6] == 10 &&
                   buffer[end - 5] == 48 &&
                   buffer[end - 4] == 13 &&
                   buffer[end - 3] == 10 &&
                   buffer[end - 2] == 13 &&
                   buffer[end - 1] == 10;
        }
    }
}
