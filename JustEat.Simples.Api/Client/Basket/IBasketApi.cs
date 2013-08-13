using JustEat.Simples.Api.Client.Basket.Models;

namespace JustEat.Simples.Api.Client.Basket
{
    public interface IBasketApi
    {
        OrderBasketResponse GetBasket(string basketId);
    }
}
