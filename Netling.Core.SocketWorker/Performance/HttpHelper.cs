using System;
using Netling.Core.SocketWorker.Extensions;

namespace Netling.Core.SocketWorker.Performance
{
    public static class HttpHelper
    {
        public static ResponseType GetResponseType(ReadOnlySpan<byte> buffer)
        {
            if (SeekHeader(buffer, CommonStrings.HeaderContentLength.Span, out _, out _))
            {
                return ResponseType.ContentLength;
            }

            if (SeekHeader(buffer, CommonStrings.HeaderTransferEncoding.Span, out _, out _))
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
