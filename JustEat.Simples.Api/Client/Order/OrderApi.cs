using System.Net;
using JustEat.Simples.Common.DataModels.OrderContainer;

namespace JustEat.Simples.Api.Client.Order
{
    public class OrderApi : ApiClientBase, IOrderApi
    {
        private readonly string _jeFeatureHeader;

        public OrderApi(IApiSettings apiSettings, string jeFeatureHeader) : base(apiSettings)
        {
            _jeFeatureHeader = jeFeatureHeader;
        }

        protected override bool CamelCasePropertyNames
        {
            get { return true; }
        }

        protected override WebRequest SetUpWebRequest(WebRequest request)
        {
            request = base.SetUpWebRequest(request);
            request.Headers.Add("X-JE-Feature", _jeFeatureHeader);
            return request;
        }

        private static class Operations
        {
            public const string Details = "/order/{0}";
            public const string Refund = "/refund";
        }

        public bool UpdateOrderStatus(int orderId, OrderStatus status, string adminComment, bool sendConfEmailToCustomer)
        {
            throw new System.NotImplementedException();
        }

        public OrderContainer OrderDetails(string orderId)
        {
            var url = BuildUrl(Operations.Details, orderId);
            var result = GetJson<OrderContainer>(url);
            return result;
        }

        public void Refund(int orderId)
        {
            var url = BuildUrl(Operations.Refund);
            GetJson<dynamic>(url, new { OrderId = orderId});
        }
    }
}