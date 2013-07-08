using System.Collections.Generic;
using JustEat.Simples.Api.Client.Restaurant.Models;

namespace JustEat.Simples.Api.Client.Restaurant
{
    public interface IRestaurantApi
    {
        IList<RestaurantDetail> GetRestaurantDetails(IEnumerable<int> restaurantIds);
    }
}