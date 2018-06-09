using System.Text;
using NUnit.Framework;

namespace Netling.Tests
{
    [TestFixture]  
    public class HttpWorkerClientTest
    {
        private byte[] _request;
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
            var client = new FakeHttpWorkerClient(_response);
            var buffer = new byte[8192];
            client.Write(_request, 0, _request.Length);
            client.Flush();

            var length = client.Read(buffer, 0, buffer.Length);
            var response = Encoding.UTF8.GetString(buffer, 0, length);

            Assert.AreEqual(132, length);
            Assert.AreEqual(_response, response);
        }

        [Test]
        public void ReadOneRequestSplit()
        {
            var client = new FakeHttpWorkerClient("HTTP/1.1 200 OK\r\nDate: Wed, 06 Jul 2016 18:26:27 GMT\r\nContent-Length: 13\r\nContent-Type: text/plain\r\nServer: Kestrel\r\n\r\n", "Hello, World!");
            var buffer = new byte[8192];
            client.Write(_request, 0, _request.Length);
            client.Flush();

            var length = client.Read(buffer, 0, buffer.Length);
            length += client.Read(buffer, length, buffer.Length);
            var response = Encoding.UTF8.GetString(buffer, 0, length);

            Assert.AreEqual(132, length);
            Assert.AreEqual(_response, response);
        }
    }
}
