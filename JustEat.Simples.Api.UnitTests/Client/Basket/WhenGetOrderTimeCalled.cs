using System;
using JustEat.Simples.Api.Client;
using JustEat.Simples.Api.Client.Basket;
using JustEat.Simples.Api.Client.Basket.Models;
using JustEat.Testing;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System.Net;

namespace JustEat.Simples.Api.UnitTests.Client.Basket
{
    public class WhenGetOrderTimeCalled : BehaviourTest<BasketApi>
    {
        private OrderTime _actual;
        private OrderTime _expected;

        protected override void Given()
        {
            _expected = new OrderTime() { Asap = true, Orderable = false, DateTime = new DateTime(2013, 12, 08, 17, 30, 00) };
        }

        protected override BasketApi CreateSystemUnderTest()
        {
            var apiSettingsMock = new Mock<IApiSettings>();

            var basketApiMock = new Mock<BasketApi>(apiSettingsMock.Object) { CallBase = true };

            object[] buildUrlParams = { ItExpr.IsAny<string>(), ItExpr.IsAny<object[]>() };
            basketApiMock.Protected()
                         .Setup<string>("BuildUrl", buildUrlParams)
                         .Returns("http://example.org/basket/anything");

            var response = new Mock<HttpWebResponse>();
            response.Setup(rs => rs.StatusCode).Returns(HttpStatusCode.OK);

            var request = new Mock<HttpWebRequest>();
            request.Setup(c => c.GetResponse()).Returns(response.Object);

            basketApiMock.Protected().Setup<WebRequest>("CreateWebRequest", ItExpr.IsAny<string>()).Returns(request.Object);

            basketApiMock.Protected().Setup<string>("GetResponseString", ItExpr.IsAny<HttpWebResponse>()).Returns(
            "{" +
            "    \"DateTime\" : \"2013-12-08T17:30:00+00:00\"," +
            "    \"Orderable\" : false," +
            "    \"Asap\" : true," +
            "    \"UserPrompt\" : [{" +
            "            \"DefaultMessage\" : \"Basket requires meal item\"," +
            "            \"StatusCode\" : \"RequiresOther\"," +
            "            \"MessageValues\" : []" +
            "        }, {" +
            "            \"DefaultMessage\" : \"Minimum order £10.00 not reached\"," +
            "            \"StatusCode\" : \"SpendMore\"," +
            "            \"MessageValues\" : [{" +
            "                    \"KeyName\" : \"[value]\"," +
            "                    \"DisplayValue\" : \"10.00\"" +
            "                }" +
            "            ]" +
            "        }" +
            "    ]" +
            "}");

            return basketApiMock.Object;
        }

        protected override void When()
        {
            _actual = SystemUnderTest.GetOrderTime("anything");
        }

        [Then]
        public void ResultIsExpectedValue()
        {
            Assert.AreEqual(_actual.Asap, _expected.Asap);
            Assert.AreEqual(_actual.DateTime, _expected.DateTime);
            Assert.AreEqual(_actual.Orderable, _expected.Orderable);
        }
    }
}