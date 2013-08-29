using System.IO;
using System.Text;
using JustEat.Simples.Api.Client;
using JustEat.Simples.Api.Client.Menu;
using JustEat.Simples.Api.Client.Menu.Models;
using JustEat.Simples.Api.Client.Order;
using JustEat.Testing;
using Moq;
using Moq.Protected;
using System.Net;

namespace JustEat.Simples.Api.UnitTests.Client.Order
{
    public class WhenRefundCalled : BehaviourTest<OrderApi>
    {
        private Mock<HttpWebRequest> _request;

        protected override void Given()
        {
        }

        protected override OrderApi CreateSystemUnderTest()
        {
            var apiSettingsMock = new Mock<IApiSettings>();

            var orderApiMock = new Mock<OrderApi>(apiSettingsMock.Object, "guard");

            object[] buildUrlParams = { ItExpr.IsAny<string>(), ItExpr.IsAny<object[]>() };
            orderApiMock.Protected()
                         .Setup<string>("BuildUrl", buildUrlParams)
                         .Returns("http://example.org/order/anything");

            var response = new Mock<HttpWebResponse>();
            response.Setup(rs => rs.StatusCode).Returns(HttpStatusCode.OK);

            _request = new Mock<HttpWebRequest>();
            _request.Setup(c => c.GetRequestStream()).Returns(new MemoryStream());
            _request.Setup(c => c.GetResponse()).Returns(response.Object);

            orderApiMock.Protected().Setup<WebRequest>("CreateWebRequest", ItExpr.IsAny<string>()).Returns(_request.Object);

            return orderApiMock.Object;
        }

        protected override void When()
        {
            SystemUnderTest.Refund(1);
        }

    }
}