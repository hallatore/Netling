using System.Net.Sockets;

namespace Netling.Core.Performance
{
    public static class HttpHelperChunked
    {
        public static int ReadChunkedStream(TcpClient client)
        {
            var buffer = new byte[4096];
            var length = 0;

            do
            {
                var read = client.Client.Receive(buffer);
                length += read;

                if (IsEndOfChunkedStream(read, buffer))
                    break;

            } while (true);

            return length;
        }

        public static bool IsEndOfChunkedStream(int read, byte[] buffer)
        {
            if (read < 7)
                return true;

            return buffer[read - 7] == 13 &&
                   buffer[read - 6] == 10 &&
                   buffer[read - 5] == 48 &&
                   buffer[read - 4] == 13 &&
                   buffer[read - 3] == 10 &&
                   buffer[read - 2] == 13 &&
                   buffer[read - 1] == 10;
        }
    }
}
