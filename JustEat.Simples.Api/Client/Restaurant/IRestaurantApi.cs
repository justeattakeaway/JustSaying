using System.Collections.Generic;
using JustEat.Simples.Api.Client.Restaurant.Models;

namespace JustEat.Simples.Api.Client.Restaurant
{
    public interface IRestaurantApi
    {
        IList<RestaurantDetail> GetRestaurantDetails(IEnumerable<int> restaurantIds);
        RestaurantDetail GetRestaurantDetails(int restaurantId);
        IList<OperationalStatus> GetRestaurantOperationalStatus(IEnumerable<int> restaurantIds);
        OperationalStatus GetRestaurantOperationalStatus(int restaurantId);
    }
}