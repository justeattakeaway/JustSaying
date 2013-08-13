using JustEat.Simples.Api.Client.Basket.Models;
namespace JustEat.Simples.Api.Client.Basket
{
    public class BasketApi : ApiClientBase, IBasketApi
    {
        public BasketApi(ApiSettings apiSettings)
            : base(apiSettings)
        {
        }

        public OrderBasketResponse GetBasket(string basketId)
        {
            string url = BuildUrl(Operations.Details, string.Join(",", basketId));

            var result = GetJson<OrderBasketResponse>(url);

            return result;
        }

        private static class Operations
        {
            public const string Details = "baskets/{0}";
        }
    }
}
