using JustEat.Simples.Api.Client;
using JustEat.Simples.Api.Client.Menu;
using JustEat.Simples.Api.Client.Menu.Models;
using JustEat.Testing;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System.Net;

namespace JustEat.Simples.Api.UnitTests.Client.Menu
{
    public class WhenGetProductDetailsCalled : BehaviourTest<MenuApi>
    {
        private ProductDetailsResponse _actual;
        private ProductDetailsResponse _expected;

        protected override void Given()
        {
            _expected = new ProductDetailsResponse() { MenuCardId = 83177 };
        }

        protected override MenuApi CreateSystemUnderTest()
        {
            var apiSettingsMock = new Mock<IApiSettings>();

            var menuApiMock = new Mock<MenuApi>(apiSettingsMock.Object);

            object[] buildUrlParams = { ItExpr.IsAny<string>(), ItExpr.IsAny<object[]>() };
            menuApiMock.Protected()
                         .Setup<string>("BuildUrl", buildUrlParams)
                         .Returns("http://example.org/menu/anything");

            var response = new Mock<HttpWebResponse>();
            response.Setup(rs => rs.StatusCode).Returns(HttpStatusCode.OK);

            var request = new Mock<HttpWebRequest>();
            request.Setup(c => c.GetResponse()).Returns(response.Object);

            menuApiMock.Protected().Setup<WebRequest>("CreateWebRequest", ItExpr.IsAny<string>()).Returns(request.Object);

            menuApiMock.Protected().Setup<string>("GetResponseString", ItExpr.IsAny<HttpWebResponse>()).Returns("{" +
                                                                                                                  "    \"ProductDetails\" : [{" +
                                                                                                                  "            \"ProductId\" : 6924545," +
                                                                                                                  "            \"Price\" : 2.45," +
                                                                                                                  "            \"Name\" : \"Samosa\"," +
                                                                                                                  "            \"Synonym\" : \"Meat\"," +
                                                                                                                  "            \"ProductTypeId\" : 1," +
                                                                                                                  "            \"RequireOtherProducts\" : false," +
                                                                                                                  "            \"Offer\" : \"NoOffer\"," +
                                                                                                                  "            \"OptionalAccessories\" : []," +
                                                                                                                  "            \"RequiredAccessories\" : []," +
                                                                                                                  "            \"MealParts\" : []" +
                                                                                                                  "        }" +
                                                                                                                  "    ]," +
                                                                                                                  "    \"MenuCardId\" : 83177" +
                                                                                                                  "}" );

            return menuApiMock.Object;
        }

        protected override void When()
        {
            _actual = SystemUnderTest.GetProductDetails("anything", new []{ "productId1", "productId2" } );
        }

        [Then]
        public void ResultIsExpectedValue()
        {
            Assert.AreEqual(_actual.MenuCardId, _expected.MenuCardId);
        }
    }
}