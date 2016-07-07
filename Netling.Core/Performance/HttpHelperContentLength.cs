using System.Net.Sockets;

namespace Netling.Core.Performance
{
    public static class HttpHelperContentLength
    {
        public static int GetContentLength(byte[] buffer)
        {
            int index;
            int length;
            HttpHelper.SeekHeader(buffer, HttpHeaders.ContentLength, 0, out index, out length);
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

        public static int ReadContentLengthStream(TcpClient client, int length, int end)
        {
            var buffer = new byte[4096];

            do
            {
                length += client.Client.Receive(buffer);
            } while (length < end);

            return length;
        }
    }
}
