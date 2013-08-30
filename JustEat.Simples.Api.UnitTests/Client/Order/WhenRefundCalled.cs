using System.IO;
using System.Linq;
using System.Text;
using JustEat.Simples.Api.Client;
using JustEat.Simples.Api.Client.Menu;
using JustEat.Simples.Api.Client.Menu.Models;
using JustEat.Simples.Api.Client.Order;
using JustEat.Testing;
using Moq;
using Moq.Protected;
using System.Net;
using NUnit.Framework;

namespace JustEat.Simples.Api.UnitTests.Client.Order
{
    public class WhenRefundCalled : BehaviourTest<OrderApi>
    {
        private Mock<HttpWebRequest> _request;
        private byte[] _buffer;
        private string _jeFeature;
        private WebHeaderCollection _requestHeaders;

        protected override void Given()
        {
        }

        protected override OrderApi CreateSystemUnderTest()
        {
            var apiSettingsMock = new Mock<IApiSettings>();

            _jeFeature = "guard";
            var orderApiMock = new Mock<OrderApi>(apiSettingsMock.Object, _jeFeature) { CallBase = true };

            object[] buildUrlParams = { ItExpr.IsAny<string>(), ItExpr.IsAny<object[]>() };
            orderApiMock.Protected()
                         .Setup<string>("BuildUrl", buildUrlParams)
                         .Returns("http://example.org/order/anything");

            orderApiMock.Protected().SetupGet<bool>("CamelCasePropertyNames").Returns(true);

            var response = new Mock<HttpWebResponse>();
            response.Setup(rs => rs.StatusCode).Returns(HttpStatusCode.OK);
            response.Setup(rs => rs.GetResponseStream()).Returns(new MemoryStream());

            _buffer = new byte[20];
            _requestHeaders = new WebHeaderCollection();

            _request = new Mock<HttpWebRequest>();
            _request.Setup(c => c.GetRequestStream()).Returns(new MemoryStream(_buffer));
            _request.Setup(c => c.GetResponse()).Returns(response.Object);
            _request.SetupProperty(x => x.Headers);
            
            orderApiMock.Protected().Setup<WebRequest>("CreateWebRequest", ItExpr.IsAny<string>()).Returns(_request.Object);

            return orderApiMock.Object;
        }

        protected override void When()
        {
            SystemUnderTest.Refund(1);
        }

        [Then]
        public void BodyJsonIsAsExpected()
        {
            var trimmedBuffer = _buffer.ToList();
            trimmedBuffer.RemoveAll(b => b == new byte());
            var body = Encoding.UTF8.GetString(trimmedBuffer.ToArray());
            Assert.AreEqual("{\"orderId\":1}", body);
        }

        [Then]
        public void JeFeatureHeaderIsPresent()
        {
            Assert.AreSame(_request.Object.Headers["X-JE-Feature"], _jeFeature);
        }
    }
}