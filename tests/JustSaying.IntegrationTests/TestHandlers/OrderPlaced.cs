using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests.TestHandlers
{
    public class OrderPlaced : Message
    {
        public OrderPlaced(string orderId)
        {
            OrderId = orderId;
        }

        public string OrderId { get; private set; }
    }
}
