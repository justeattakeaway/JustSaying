using JustSaying.IntegrationTests.JustSayingFluently;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
#pragma warning disable 618
    public class BlockingOrderProcessor : IHandler<OrderPlaced>
#pragma warning restore 618
    {
        private readonly Future<OrderPlaced> _future;

        public BlockingOrderProcessor(Future<OrderPlaced> future)
        {
            _future = future;
        }

        public bool Handle(OrderPlaced message)
        {
            _future.Complete(message).Wait();
            return true;
        }

        public Future<OrderPlaced> Future
        {
            get { return _future; }
        }
    }
}