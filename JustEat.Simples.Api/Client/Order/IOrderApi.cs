using JustEat.Simples.Common.DataModels.OrderContainer;

namespace JustEat.Simples.Api.Client.Order
{
    public interface IOrderApi
    {
        bool UpdateOrderStatus(int orderId, OrderStatus status, string adminComment, bool sendConfEmailToCustomer);
        OrderContainer OrderDetails(string orderId);
    }
}