using System;
using Netling.Core.SocketWorker.Extensions;

namespace Netling.Core.SocketWorker.Performance
{
    public static class HttpHelper
    {
        public static ResponseType GetResponseType(ReadOnlySpan<byte> buffer)
        {
            if (buffer.IndexOf(CommonStrings.HeaderContentLength.Span) != -1)
            {
                return ResponseType.ContentLength;
            }

            if (buffer.IndexOf(CommonStrings.HeaderTransferEncoding.Span) != -1)
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
            index = buffer.IndexOf(header);

            if (index < 0)
            {
                length = 0;
                return false;
            }

            index += header.Length;
            length = buffer.Slice(index).IndexOf(CommonStrings.HeaderReturn.Span);
            return true;
        }
    }
}
