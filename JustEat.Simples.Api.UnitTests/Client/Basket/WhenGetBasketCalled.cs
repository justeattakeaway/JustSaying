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
    public class WhenGetBasketCalled : BehaviourTest<BasketApi>
    {
        private OrderBasketResponse _actual;
        private OrderBasketResponse _expected;

        protected override void Given()
        {
            _expected = new OrderBasketResponse() { MenuId = 83177 };
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
            "   \"Id\" : \"xWcFLstm9kKnxno88qpnRA\"," +
            "   \"SubTotal\" : 157.58," +
            "   \"UserPrompt\" : [{" +
            "           \"DefaultMessage\" : \"Hurray! You saved £1.00\"," +
            "           \"StatusCode\" : \"Discount\"," +
            "           \"MessageValues\" : [{" +
            "                   \"KeyName\" : \"[value]\"," +
            "                   \"DisplayValue\" : \"1.00\"" +
            "               }" +
            "           ]" +
            "       }" +
            "   ]," +
            "   \"OrderItems\" : [{" +
            "           \"OrderItemId\" : \"7hWb6I4jjkOL4EeHxEImHg\"," +
            "           \"ProductId\" : 6924578," +
            "           \"UnitPrice\" : 4.99," +
            "           \"CombinedPrice\" : 4.99," +
            "           \"Name\" : null," +
            "           \"MealParts\" : [{" +
            "                   \"Id\" : 6924585," +
            "                   \"GroupId\" : 1," +
            "                   \"OptionalAccessories\" : []," +
            "                   \"RequiredAccessories\" : []" +
            "               }" +
            "           ]," +
            "           \"OptionalAccessories\" : []," +
            "           \"RequiredAccessories\" : []," +
            "           \"Synonym\" : null," +
            "           \"ProductTypeId\" : 409" +
            "       }, {" +
            "           \"OrderItemId\" : \"SmlHVAVDp0SHc1q8ZryABw\"," +
            "           \"ProductId\" : 6924577," +
            "           \"UnitPrice\" : 4.5," +
            "           \"CombinedPrice\" : 4.5," +
            "           \"Name\" : null," +
            "           \"MealParts\" : []," +
            "           \"OptionalAccessories\" : []," +
            "           \"RequiredAccessories\" : [{" +
            "                   \"Id\" : 800," +
            "                   \"GroupId\" : 3," +
            "                   \"UnitPrice\" : 0.0" +
            "               }" +
            "           ]," +
            "           \"Synonym\" : null," +
            "           \"ProductTypeId\" : 409" +
            "       }, {" +
            "           \"OrderItemId\" : \"DMaDjEJybUu6RoswaFy0oQ\"," +
            "           \"ProductId\" : 6924570," +
            "           \"UnitPrice\" : 8.0," +
            "           \"CombinedPrice\" : 13.0," +
            "           \"Name\" : null," +
            "           \"MealParts\" : []," +
            "           \"OptionalAccessories\" : [{" +
            "                   \"Id\" : 55," +
            "                   \"Quantity\" : 1," +
            "                   \"UnitPrice\" : 1.0" +
            "               }, {" +
            "                   \"Id\" : 41," +
            "                   \"Quantity\" : 1," +
            "                   \"UnitPrice\" : 1.0" +
            "               }, {" +
            "                   \"Id\" : 9," +
            "                   \"Quantity\" : 1," +
            "                   \"UnitPrice\" : 1.0" +
            "               }, {" +
            "                   \"Id\" : 42," +
            "                   \"Quantity\" : 1," +
            "                   \"UnitPrice\" : 1.0" +
            "               }, {" +
            "                   \"Id\" : 34," +
            "                   \"Quantity\" : 1," +
            "                   \"UnitPrice\" : 1.0" +
            "               }" +
            "           ]," +
            "           \"RequiredAccessories\" : []," +
            "           \"Synonym\" : null," +
            "           \"ProductTypeId\" : 89" +
            "       }" +
            "   ]," +
            "   \"MenuId\" : 83177," +
            "   \"ToSpend\" : 0.0," +
            "   \"MultiBuyDiscount\" : 1.0," +
            "   \"Discount\" : 0.0," +
            "   \"DeliveryCharge\" : 2.0," +
            "   \"Total\" : 158.58," +
            "   \"Orderable\" : true," +
            "   \"ServiceType\" : \"Delivery\"," +
            "   \"RestaurantId\" : 24310" +
            "}");

            return basketApiMock.Object;
        }

        protected override void When()
        {
            _actual = SystemUnderTest.GetBasket("anything");
        }

        [Then]
        public void ResultIsExpectedValue()
        {
            Assert.AreEqual(_actual.MenuId, _expected.MenuId);
        }
    }
}