using System.Numerics;
using Netling.Core.Utils;

namespace Netling.Core.Performance
{
    internal static class HttpHelperChunked
    {
        public static bool IsEndOfChunkedStream(byte[] buffer, int end)
        {
            if (end < 5)
                return true;

            return buffer[end - 5] == 48 &&
                   buffer[end - 4] == 13 &&
                   buffer[end - 3] == 10 &&
                   buffer[end - 2] == 13 &&
                   buffer[end - 1] == 10;
        }

        private static Vector<short> _zeroReturnVector = new Vector<short>(48 + (13 << 8));
        private static Vector<short> _zeroReturnVectorFiller = new Vector<short>(48 + (13 << 8));

        public static int SeekEndOfChunkedStream(byte[] buffer, int start, int end)
        {
            while (start + 4 < end)
            {
                if (!ByteHelpers.ContainsPossibly(buffer, start, end, ref _zeroReturnVector))
                {
                    start += Vector<byte>.Count;
                    continue;
                }

                var c = 0;

                while (start + 4 < end && c < Vector<byte>.Count)
                {
                    if (buffer[start + 0] == 48 &&
                        buffer[start + 1] == 13 &&
                        buffer[start + 2] == 10 &&
                        buffer[start + 3] == 13 &&
                        buffer[start + 4] == 10)
                        return start + 5;

                    c++;
                    start++;
                }
            }

            return -1;
        }
    }
}
