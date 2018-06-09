using System.Runtime.CompilerServices;
using Netling.Core.SocketWorker.Extensions;

namespace Netling.Core.SocketWorker.Performance
{
    public static class HttpHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ResponseType GetResponseType(byte[] buffer, int start, int end)
        {
            if (SeekHeader(buffer, HttpHeaders.ContentLength, start, end, out _, out _))
                return ResponseType.ContentLength;

            if (SeekHeader(buffer, HttpHeaders.TransferEncoding, start, end, out _, out _))
                return ResponseType.Chunked;

            return ResponseType.Unknown;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetStatusCode(byte[] buffer, int start, int end)
        {
            return ByteExtensions.ConvertToInt(buffer, start + 9, 3, end);
        }

        // HTTP/1.1 200 OK\r\nDate: Wed, 06 Jul 2016 18:26:27 GMT\r\nContent-Length: 13\r\nContent-Type: text/plain\r\nServer: Kestrel\r\n\r\nHello, World!
        public static bool SeekHeader(byte[] buffer, byte[] header, int start, int end, out int index, out int length)
        {
            if (end < start + header.Length)
            {
                index = -1;
                length = 0;
                return false;
            }

            for (var i = 0; i < header.Length; i++)
            {
                if (start + i < end)
                {
                    if ((header[i] | 0x20) == (buffer[start + i] | 0x20))
                        continue;

                    var next = SeekReturn(buffer, start + i, end);

                    if (next != -1)
                        return SeekHeader(buffer, header, next, end, out index, out length);
                }

                index = -1;
                length = 0;
                return false;
            }

            index = start + header.Length;
            var r = SeekReturn(buffer, index, end);
            length = r - index;
            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SeekReturn(byte[] buffer, int start, int end)
        {
            while (start + 1 < end)
            {
                if (buffer[start] == 13)
                    return start;

                start++;
            }

            return -1;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SeekHeaderEnd(byte[] buffer, int start, int end)
        {
            while (start + 3 < end)
            {
                if (buffer[start] == 13 &&
                    buffer[start + 1] == 10 &&
                    buffer[start + 2] == 13 &&
                    buffer[start + 3] == 10)
                    return start;

                start++;
            }

            return -1;
        }
    }
}
