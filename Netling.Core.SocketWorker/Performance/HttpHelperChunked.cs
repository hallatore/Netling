using System;

namespace Netling.Core.SocketWorker.Performance
{
    public static class HttpHelperChunked
    {
        public static bool IsEndOfChunkedStream(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length < 5)
                return true;

            return buffer[buffer.Length - 5] == 48 &&
                   buffer[buffer.Length - 4] == 13 &&
                   buffer[buffer.Length - 3] == 10 &&
                   buffer[buffer.Length - 2] == 13 &&
                   buffer[buffer.Length - 1] == 10;
        }

        public static int SeekEndOfChunkedStream(ReadOnlySpan<byte> buffer)
        {
            var start = 0;

            while (start + 4 < buffer.Length)
            {
                if (buffer[start + 0] == 48 &&
                    buffer[start + 1] == 13 &&
                    buffer[start + 2] == 10 &&
                    buffer[start + 3] == 13 &&
                    buffer[start + 4] == 10)
                    return start + 5;

                start++;
            }

            return -1;
        }
    }
}
