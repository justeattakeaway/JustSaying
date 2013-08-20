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

        private static class Operations
        {
            public const string Baskets = "baskets/{0}";
        }
    }
}
