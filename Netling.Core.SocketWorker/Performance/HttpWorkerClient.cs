using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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
            if (_client != null && _client.Connected)
                return;

            _client?.Close();
            _client = new TcpClient();

            try
            {
                const int sioLoopbackFastPath = -1744830448;
                var optionInValue = BitConverter.GetBytes(1);
                _client.Client.IOControl(sioLoopbackFastPath, optionInValue, null);
            }
            catch (SocketException) { }
            
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
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte[] buffer, int offset, int count)
        {
            CheckInit();
            Stream.Write(buffer, offset, count);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Flush()
        {
            Stream.Flush();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read(byte[] buffer, int offset, int count)
        {
            return Stream.Read(buffer, offset, count);
        }

        public void Dispose()
        {
            _client?.Close();
        }
    }
}