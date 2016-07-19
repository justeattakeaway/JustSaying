using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.IntegrationTests.TestHandlers
{
    public class OrderDispatcher : IHandlerAsync<OrderPlaced>
    {
        private readonly Future<OrderPlaced> _future;

        public OrderDispatcher(Future<OrderPlaced> future)
        {
            _future = future;
        }

        public async Task<bool> Handle(OrderPlaced message)
        {
            await _future.Complete(message);
            return true;
        }

        public Future<OrderPlaced> Future => _future;
    }
}