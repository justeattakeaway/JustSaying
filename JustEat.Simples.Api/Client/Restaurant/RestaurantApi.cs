using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using JustEat.Simples.Api.Client.Restaurant.Models;

namespace JustEat.Simples.Api.Client.Restaurant
{
    public class RestaurantApi : ApiClientBase, IRestaurantApi
    {
        public RestaurantApi(ApiSettings apiSettings)
            : base(apiSettings)
        {
        }

        public IList<RestaurantDetail> GetRestaurantDetails(IEnumerable<int> restaurantIds)
        {
            string url = BuildUrl(Operations.Details, String.Join(",", restaurantIds));

            var results = GetJson<dynamic>(url);

            return ((IEnumerable<dynamic>)results.Details)
                .Select(x =>
                        new RestaurantDetail
                            {
                                RestaurantId = (dynamic)x.Id,
                                Name = x.Name,
                                PhoneNumber = x.PhoneNumber,
                                RestaurantType = x.RestaurantType
                            }).ToList();
        }

        public RestaurantDetail GetRestaurantDetails(int restaurantId)
        {
            return GetRestaurantDetails(new[] {restaurantId}).Single();
        }

        public IList<OperationalStatus> GetRestaurantOperationalStatus(IEnumerable<int> restaurantIds)
        {
            string url = BuildUrl(Operations.Confidence, String.Join(",", restaurantIds));
            var result = GetJson<IList<OperationalStatus>>(url);
            return result;
        }

        public OperationalStatus GetRestaurantOperationalStatus(int restaurantId)
        {
            return GetRestaurantOperationalStatus(new[] {restaurantId}).Single();
        }

        public dynamic JctStatusExpando(int restaurantId, string imei)
        {
            dynamic expando = new ExpandoObject();
            expando.RestaurantId = restaurantId;
            expando.IMEI = imei;
            return expando;
        }

        private static class Operations
        {
            public const string Details = "/details?ids={0}";
            public const string Confidence = "/operationalStatus?ids={0}";
        }
    }
}