using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Sample.Restaurant.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Sample.Restaurant.OrderingApi.Handlers
{
    public class OrderReadyEventHandler : IHandlerAsync<OrderReadyEvent>
    {
        private readonly ILogger<OrderReadyEventHandler> _log;

        public OrderReadyEventHandler(ILogger<OrderReadyEventHandler> log)
        {
            _log = log;
        }

        public Task<bool> Handle(OrderReadyEvent message)
        {
            _log.LogInformation("Order {orderId} ready", message.OrderId);

            // This is where you would actually handle the order placement
            // Intentionally left empty for the sake of this being a sample application

            // Returning true would indicate:
            //   The message was handled successfully
            //   The message can be removed from the queue.
            // Returning false would indicate:
            //   The message was not handled successfully
            //   The message handling should be retried (configured by default)
            //   The message should be moved to the error queue if all retries fail
            return Task.FromResult(true);
        }
    }
}
