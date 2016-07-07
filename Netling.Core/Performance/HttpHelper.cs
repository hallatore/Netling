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
        

        public static ResponseType GetResponseType(byte[] buffer)
        {
            int index;
            int length;

            if (SeekHeader(buffer, HttpHeaders.ContentLength, 0, out index, out length))
                return ResponseType.ContentLength;

            if (SeekHeader(buffer, HttpHeaders.TransferEncoding, 0, out index, out length))
                return ResponseType.Chunked;

            return ResponseType.Unknown;
        }

        // HTTP/1.1 200 OK\r\nDate: Wed, 06 Jul 2016 18:26:27 GMT\r\nContent-Length: 13\r\nContent-Type: text/plain\r\nServer: Kestrel\r\n\r\nHello, World!
        public static bool SeekHeader(byte[] buffer, byte[] header, int start, out int index, out int length)
        {
            if (start + header.Length > buffer.Length)
            {
                index = -1;
                length = 0;
                return false;
            }

            for (var i = 0; i < header.Length; i++)
            {
                if (header[i] == buffer[start + i])
                    continue;

                var next = SeekReturn(buffer, start + i);

                if (next != -1)
                    return SeekHeader(buffer, header, next, out index, out length);

                index = -1;
                length = 0;
                return false;
            }

            index = start + header.Length;
            var end = SeekReturn(buffer, index);
            length = end - index;
            return true;
        }

        private static int SeekReturn(byte[] buffer, int i)
        {
            while (buffer.Length > i + 1)
            {
                if (buffer[i] == 13)
                    return i;

                i++;
            }

            return -1;
        }
        
        public static int SeekHeaderEnd(byte[] buffer, int i)
        {
            while (buffer.Length > i + 3)
            {
                if (buffer[i] == 13 &&
                    buffer[i + 1] == 10 &&
                    buffer[i + 2] == 13 &&
                    buffer[i + 3] == 10)
                    return i;

                i++;
            }

            return -1;
        }

        public static bool IsHeaderStart(byte[] buffer, int i)
        {
            if (i + 3 > buffer.Length)
                return false;

            return buffer[i] == 72 &&
                   buffer[i + 1] == 84 &&
                   buffer[i + 2] == 84 &&
                   buffer[i + 3] == 80;
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
