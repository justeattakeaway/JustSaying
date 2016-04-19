using System.Threading.Tasks;
using JustSaying.IntegrationTests.JustSayingFluently;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class OrderPlaced : Message
    {
        public OrderPlaced(string orderId)
        {
            OrderId = orderId;
        }
        public string OrderId { get; private set; }
    }

    public class OrderProcessor : IAsyncHandler<OrderPlaced>
    {
        private readonly Future<OrderPlaced> _future;

        public OrderProcessor(Future<OrderPlaced> future)
        {
            _future = future;
        }

        public async Task<bool> Handle(OrderPlaced message)
        {
            await _future.Complete(message);
            return true;
        }

        public Future<OrderPlaced> Future
        {
            get { return _future; }
        }
    }

    public class OrderDispatcher : IAsyncHandler<OrderPlaced>
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

        public Future<OrderPlaced> Future
        {
            get { return _future; }
        }
    }
}