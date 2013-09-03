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
    public class WhenGetOrderAddressCalled : BehaviourTest<BasketApi>
    {
        private OrderAddress _actual;
        private OrderAddress _expected;

        protected override void Given()
        {
            _expected = new OrderAddress
                {
                    Address = "123 fake st",
                    City = "springfield",
                    Email = "hello@not.com",
                    PhoneNumber = "789783736",
                    PostCode = "AR51 7ET"
                };
        }

        protected override BasketApi CreateSystemUnderTest()
        {
            var apiSettingsMock = new Mock<IApiSettings>();

            var basketApiMock = new Mock<BasketApi>(apiSettingsMock.Object) { CallBase = true };

            object[] buildUrlParams = {ItExpr.IsAny<string>(), ItExpr.IsAny<object[]>()};
            basketApiMock.Protected()
                         .Setup<string>("BuildUrl", buildUrlParams)
                         .Returns("http://example.org/basket/anything");

            var response = new Mock<HttpWebResponse>();
            response.Setup(rs => rs.StatusCode).Returns(HttpStatusCode.OK);

            var request = new Mock<HttpWebRequest>();
            request.Setup(c => c.GetResponse()).Returns(response.Object);

            basketApiMock.Protected()
                         .Setup<WebRequest>("CreateWebRequest", ItExpr.IsAny<string>())
                         .Returns(request.Object);

            basketApiMock.Protected().Setup<string>("GetResponseString", ItExpr.IsAny<HttpWebResponse>()).Returns(
                "{" +
                "    \"Address\" : \"123 fake st\"," +
                "    \"City\" : \"springfield\"," +
                "    \"Email\" : \"hello@not.com\"," +
                "    \"PhoneNumber\" : \"789783736\"," +
                "    \"PostCode\" : \"AR51 7ET\"," +
                "    \"Orderable\" : false," +
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
                "        }, {" +
                "            \"DefaultMessage\" : \"Spend £10.00 more to get 20% OFF\"," +
                "            \"StatusCode\" : \"Offer\"," +
                "            \"MessageValues\" : [{" +
                "                    \"KeyName\" : \"[value]\"," +
                "                    \"DisplayValue\" : \"10.00\"" +
                "                }, {" +
                "                    \"KeyName\" : \"[percent]\"," +
                "                    \"DisplayValue\" : \"20\"" +
                "                }" +
                "            ]" +
                "        }" +
                "    ]" +
                "}");

            return basketApiMock.Object;
        }

        protected override void When()
        {
            _actual = SystemUnderTest.GetOrderAddress("anything");
        }

        [Then]
        public void ResultIsExpectedValue()
        {
            Assert.AreEqual(_actual.Address, _expected.Address);
            Assert.AreEqual(_actual.City, _expected.City);
            Assert.AreEqual(_actual.Email, _expected.Email);
            Assert.AreEqual(_actual.Name, _expected.Name);
            Assert.AreEqual(_actual.Orderable, _expected.Orderable);
            Assert.AreEqual(_actual.PhoneNumber, _expected.PhoneNumber);
            Assert.AreEqual(_actual.PostCode, _expected.PostCode);
        }
    }
}