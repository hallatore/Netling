using System;
using Netling.Core.SocketWorker.Performance;
using NUnit.Framework;

namespace Netling.Tests
{
    [TestFixture]  
    public class HttpWorkerTest
    {
        [Test]
        public void ReadResponse()
        {
            var client = (IHttpWorkerClient)new FakeHttpWorkerClient("HTTP/1.1 200 OK\r\nDate: Wed, 06 Jul 2016 18:26:27 GMT\r\nContent-Length: 13\r\nContent-Type: text/plain\r\nServer: Kestrel\r\n\r\nHello, World!");
            var httpWorker = new HttpWorker(client, new Uri("http://netling.test", UriKind.Absolute));

            var length = httpWorker.Read(out var statusCode);
            Assert.AreEqual(200, statusCode);
            Assert.AreEqual(132, length);
        }
        
        [Test]
        public void ReadResponseSplit()
        {
            var client = (IHttpWorkerClient)new FakeHttpWorkerClient("HTTP/1.1 200 OK\r\nDate: Wed, 06 Jul 2016 18:26:27 GMT\r\nContent-Length: 13\r\nContent-Type: text/plain\r\nServer: Kestrel\r\n\r\n", "Hello, World!");
            var httpWorker = new HttpWorker(client, new Uri("http://netling.test", UriKind.Absolute));

            var length = httpWorker.Read(out var statusCode);
            Assert.AreEqual(200, statusCode);
            Assert.AreEqual(132, length);
        }
    }
}
