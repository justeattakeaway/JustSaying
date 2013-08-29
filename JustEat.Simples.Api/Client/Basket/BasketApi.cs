using JustEat.Simples.Api.Client.Basket.Models;
namespace JustEat.Simples.Api.Client.Basket
{
    public class BasketApi : ApiClientBase, IBasketApi
    {
        public BasketApi(IApiSettings apiSettings)
            : base(apiSettings)
        {
        }

        public OrderBasketResponse GetBasket(string basketId)
        {
            var url = BuildUrl(Operations.Baskets, basketId);

            var result = GetJson<OrderBasketResponse>(url);

            return result;
        }

        public OrderAddress GetOrderAddress(string basketId)
        {
            var url = BuildUrl(Operations.OrderAddress, basketId);

            var result = GetJson<OrderAddress>(url);

            return result;
        }

        public OrderTime GetOrderTime(string basketId)
        {
            var url = BuildUrl(Operations.OrderTime, basketId);

            var result = GetJson<OrderTime>(url);

            return result;
        }

        private static class Operations
        {
            public const string Baskets = "baskets/{0}";
            public const string OrderAddress = "/baskets/{0}/orderaddress";
            public const string OrderTime = "/baskets/{0}/ordertime";
        }
    }
}
