using System;
using System.Net.Sockets;
using System.Text;

namespace Netling.Core.Performance
{
    public static class HttpHelperContentLength
    {
        public static int GetContentLength(byte[] buffer)
        {
            int index;
            int length;
            HttpHelper.TryGetHeaderLocation(buffer, HttpHelper.HeaderContentLength, 0, out index, out length);

            var b = new byte[length];
            Array.Copy(buffer, index, b, 0, length);
            return ConvertToInt(b);
        }

        private static int ConvertToInt(byte[] bytes)
        {
            var result = 0;
            var length = bytes.Length;

            for (var i = 0; i < length; i++)
            {
                result = result * 10 + (bytes[i] - '0');
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
