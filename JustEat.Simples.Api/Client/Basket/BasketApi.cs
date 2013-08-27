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
            throw new System.NotImplementedException();
        }

        public OrderTime GetOrderTime(string basketId)
        {
            throw new System.NotImplementedException();
        }

        private static class Operations
        {
            public const string Baskets = "baskets/{0}";
        }
    }
}
