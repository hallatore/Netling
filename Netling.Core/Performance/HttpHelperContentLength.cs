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
            return Convert.ToInt32(Encoding.UTF8.GetString(b));
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
