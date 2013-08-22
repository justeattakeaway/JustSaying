using JustEat.Simples.Common.DataModels.OrderContainer;

namespace JustEat.Simples.Api.Client.Order
{
    public class OrderApi : ApiClientBase, IOrderApi
    {
        public OrderApi(IApiSettings apiSettings) : base(apiSettings) { }

        private static class Operations
        {
            public const string Details = "/order/{0}";
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
    }
}