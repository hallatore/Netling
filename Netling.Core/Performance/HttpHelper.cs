using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Netling.Core.Performance
{
    public static class HttpHelper
    {
        public static readonly byte[] HeaderContentLength = Encoding.UTF8.GetBytes("Content-Length: ");
        public static readonly byte[] HeaderTransferEncoding = Encoding.UTF8.GetBytes("Transfer-Encoding: ");

        public static ResponseType GetResponseType(byte[] buffer)
        {
            int index;
            int length;

            if (TryGetHeaderLocation(buffer, HeaderContentLength, 0, out index, out length))
                return ResponseType.ContentLength;

            if (TryGetHeaderLocation(buffer, HeaderTransferEncoding, 0, out index, out length))
                return ResponseType.Chunked;

            return ResponseType.Unknown;
        }

        // HTTP/1.1 200 OK\r\nDate: Wed, 06 Jul 2016 18:26:27 GMT\r\nContent-Length: 13\r\nContent-Type: text/plain\r\nServer: Kestrel\r\n\r\nHello, World!
        public static bool TryGetHeaderLocation(byte[] buffer, byte[] header, int start, out int index, out int length)
        {
            for (var i = 0; i < header.Length; i++)
            {
                if (header[i] == buffer[start + i])
                    continue;

                var next = FindReturn(buffer, start + i);

                if (next != -1)
                    return TryGetHeaderLocation(buffer, header, next + 2, out index, out length);

                index = -1;
                length = 0;
                return false;
            }

            index = start + header.Length;
            var end = FindReturn(buffer, index);
            length = end - index;
            return true;
        }

        // \r\n\r\n = 13 10 13 10
        public static int GetHeaderLength(byte[] buffer)
        {
            var i = 0;
            while (buffer.Length > i + 3)
            {
                if (buffer[i] == 13 &&
                    buffer[i + 1] == 10 &&
                    buffer[i + 2] == 13 &&
                    buffer[i + 3] == 10)
                    return i + 4;

                i++;
            }

            return -1;
        }

        // \r\n = 13 10
        public static int FindReturn(byte[] buffer, int i)
        {
            while (buffer.Length > i + 1)
            {
                if (buffer[i] == 13 &&
                    buffer[i + 1] == 10)
                    return i;

                i++;
            }

            return -1;
        }

        public static Stream GetStream(TcpClient client, Uri uri)
        {
            if (uri.Scheme == "http")
                return client.GetStream();

            var stream = new SslStream(client.GetStream());
            var xc = new X509Certificate2Collection();
            stream.AuthenticateAsClient(uri.Host, xc, SslProtocols.Tls, false);
            return stream;
        }
    }
}
