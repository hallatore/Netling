using System.Text;
using Netling.Core.SocketWorker.Extensions;
using Netling.Core.SocketWorker.Performance;
using NUnit.Framework;

namespace Netling.Tests
{
    [TestFixture]  
    public class MiscTest
    {
        private byte[] _request;
        private byte[] _response;

        [SetUp]
        protected void SetUp() 
        {
            _request = Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\nAccept-Encoding: gzip, deflate, sdch\r\nHost: test.netling\r\nContent-Length: 5\r\n\r\n12345");
            _response = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nDate: Wed, 06 Jul 2016 18:26:27 GMT\r\nContent-Length: 13\r\nContent-Type: text/plain\r\nServer: Kestrel\r\n\r\nHello, World!");
        }

        [Test]
        public void ByteExtensions_ConvertToInt()
        {
            Assert.AreEqual(5, ByteExtensions.ConvertToInt(_request, 90, 1, _request.Length));
            Assert.AreEqual(12345, ByteExtensions.ConvertToInt(_request, 95, 5,_request.Length));
        }

        [Test]
        public void HttpHelper_GetResponseType()
        {
            Assert.AreEqual(ResponseType.ContentLength, HttpHelper.GetResponseType(_response, 0, _response.Length));
        }

        [Test]
        public void HttpHelper_GetStatusCode()
        {
            Assert.AreEqual(200, HttpHelper.GetStatusCode(_response, 0, _response.Length));
        }

        [Test]
        public void HttpHelper_SeekHeader()
        {
            HttpHelper.SeekHeader(_response, HttpHeaders.ContentLength, 0, _response.Length, out var index, out var length);
            Assert.AreEqual(70, index);
            Assert.AreEqual(2, length);
        }

        [Test]
        public void HttpHelperContentLength_GetHeaderContentLength()
        {
            Assert.AreEqual(13, HttpHelperContentLength.GetHeaderContentLength(_response, 0, _response.Length));
        }

        [Test]
        public void HttpHelperContentLength_GetResponseLength()
        {
            Assert.AreEqual(132, HttpHelperContentLength.GetResponseLength(_response, 0, _response.Length));
        }
    }
}
