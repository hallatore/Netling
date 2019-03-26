using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Netling.Core.SocketWorker.Performance
{
    public class HttpWorkerClient : IHttpWorkerClient
    {
        private TcpClient _client;
        private readonly IPEndPoint _endPoint;
        private readonly Uri _uri;

        public HttpWorkerClient(IPEndPoint endPoint, Uri uri)
        {
            _endPoint = endPoint;
            _uri = uri;
            CheckInit();
        }

        public Stream Stream { get; private set; }

        private void CheckInit()
        {
            if (_client?.Connected == true)
                return;

            _client?.Close();
            _client = new TcpClient();

            try
            {
                const int sioLoopbackFastPath = -1744830448;
                var optionInValue = BitConverter.GetBytes(1);
                _client.Client.IOControl(sioLoopbackFastPath, optionInValue, null);
            }
            catch (Exception) { }
            
            _client.NoDelay = true;
            _client.SendTimeout = 10000;
            _client.ReceiveTimeout = 10000;
            _client.Connect(_endPoint);
            Stream = GetStream(_uri);
        }

        private Stream GetStream(Uri uri)
        {
            if (uri.Scheme == Uri.UriSchemeHttp)
                return _client.GetStream();
            
            var stream = new SslStream(_client.GetStream());
            var xc = new X509Certificate2Collection();
            stream.AuthenticateAsClient(uri.Host, xc, SslProtocols.Tls12, false);
            return stream;
        }
        
        public void Write(ReadOnlySpan<byte> buffer)
        {
            CheckInit();
            Stream.Write(buffer);
            Stream.Flush();
        }

        public int Read(Memory<byte> buffer)
        {
            return Stream.Read(buffer.Span);
        }

        public void Dispose()
        {
            _client?.Close();
        }
    }
}