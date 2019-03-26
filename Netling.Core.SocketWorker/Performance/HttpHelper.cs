using System;
using Netling.Core.SocketWorker.Extensions;

namespace Netling.Core.SocketWorker.Performance
{
    public static class HttpHelper
    {
        public static ResponseType GetResponseType(ReadOnlySpan<byte> buffer)
        {
            if (SeekHeader(buffer, HttpHeaders.ContentLength.Span, out _, out _))
            {
                return ResponseType.ContentLength;
            }

            if (SeekHeader(buffer, HttpHeaders.TransferEncoding.Span, out _, out _))
            {
                return ResponseType.Chunked;
            }

            return ResponseType.Unknown;
        }

        public static int GetStatusCode(ReadOnlySpan<byte> buffer)
        {
            return ByteExtensions.ConvertToInt(buffer.Slice(9, 3));
        }

        public static bool SeekHeader(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> header, out int index, out int length)
        {
            if (buffer.Length < header.Length)
            {
                index = -1;
                length = 0;
                return false;
            }

            for (var i = 0; i < header.Length; i++)
            {
                if (header[i] == buffer[i])
                {
                    continue;
                }

                var nextReturnIndex = SeekReturn(buffer.Slice(i));

                if (nextReturnIndex != -1)
                {
                    var seek = SeekHeader(buffer.Slice(i + nextReturnIndex), header, out int headerIndex, out length);
                    index = i + nextReturnIndex + headerIndex;
                    return seek;
                }

                index = -1;
                length = 0;
                return false;
            }

            index = header.Length;
            length = SeekReturn(buffer.Slice(index));
            return true;
        }
        
        private static int SeekReturn(ReadOnlySpan<byte> buffer)
        {
            var start = 0;

            while (start + 1 < buffer.Length)
            {
                if (buffer[start] == 13)
                    return start;

                start++;
            }

            return -1;
        }
        
        public static int SeekHeaderEnd(ReadOnlySpan<byte> buffer)
        {
            var start = 0;

            while (start + 3 < buffer.Length)
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
