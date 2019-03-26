using System;

namespace Netling.Core.SocketWorker.Performance
{
    public static class HttpHelperChunked
    {
        public static bool IsEndOfChunkedStream(ReadOnlySpan<byte> buffer)
        {
            return buffer.Length >= 5 && buffer.Slice(buffer.Length - 5, 5).SequenceEqual(CommonStrings.EndOfChunkedResponse.Span);
        }

        public static int SeekEndOfChunkedStream(ReadOnlySpan<byte> buffer)
        {
            var index = buffer.IndexOf(CommonStrings.EndOfChunkedResponse.Span);

            if (index < 0)
            {
                return index;
            }

            return index + CommonStrings.EndOfChunkedResponse.Span.Length;
        }
    }
}
