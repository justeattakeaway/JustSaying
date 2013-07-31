namespace JustEat.Simples.Api.Client.Order
{
    public interface IOrderApi
    {
        bool UpdateOrderStatus(int orderId, OrderStatus status, string adminComment, bool sendConfEmailToCustomer);
    }
}