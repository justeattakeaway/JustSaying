using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.IntegrationTests.TestHandlers.JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using StructureMap;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class BlockingHandlerRegistry : Registry
    {
        public BlockingHandlerRegistry()
        {
            For<IHandler<OrderPlaced>>().Singleton().Use<BlockingOrderProcessor>();
        }
    }
}
