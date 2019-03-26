using System;
using System.Text;
using Netling.Core.SocketWorker.Performance;
using NUnit.Framework;

namespace Netling.Tests
{
    [TestFixture]  
    public class HttpWorkerClientTest
    {
        private ReadOnlyMemory<byte> _request;
        private string _response;

        [SetUp]
        protected void SetUp() 
        {
            _request = Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\nAccept-Encoding: gzip, deflate, sdch\r\nHost: test.netling\r\nContent-Length: 5\r\n\r\n12345");
            _response = "HTTP/1.1 200 OK\r\nDate: Wed, 06 Jul 2016 18:26:27 GMT\r\nContent-Length: 13\r\nContent-Type: text/plain\r\nServer: Kestrel\r\n\r\nHello, World!";
        }

        [Test]
        public void ReadOneRequest()
        {
            var client = (IHttpWorkerClient)new FakeHttpWorkerClient(_response);
            var buffer = new byte[8192].AsMemory();
            client.Write(_request.Span);

            var length = client.Read(buffer);
            var response = Encoding.UTF8.GetString(buffer.ToArray(), 0, length);

            Assert.AreEqual(132, length);
            Assert.AreEqual(_response, response);
        }

        [Test]
        public void ReadOneRequestSplit()
        {
            var client = (IHttpWorkerClient)new FakeHttpWorkerClient("HTTP/1.1 200 OK\r\nDate: Wed, 06 Jul 2016 18:26:27 GMT\r\nContent-Length: 13\r\nContent-Type: text/plain\r\nServer: Kestrel\r\n\r\n", "Hello, World!");
            var buffer = new byte[8192].AsMemory();
            client.Write(_request.Span);

            var length = client.Read(buffer);
            length += client.Read(buffer.Slice(length));
            var response = Encoding.UTF8.GetString(buffer.ToArray(), 0, length);

            Assert.AreEqual(132, length);
            Assert.AreEqual(_response, response);
        }
    }
}
