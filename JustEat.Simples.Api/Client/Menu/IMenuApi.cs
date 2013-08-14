using JustEat.Simples.Api.Client.Menu.Models;
using System.Collections.Generic;

namespace JustEat.Simples.Api.Client.Menu   
{
    public interface IMenuApi
    {
        ProductDetailsResponse GetProductDetails(string menuId, IEnumerable<string> productIds);
    }
}
