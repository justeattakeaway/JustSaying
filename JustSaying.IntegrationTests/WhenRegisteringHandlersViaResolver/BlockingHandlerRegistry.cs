using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using StructureMap;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class BlockingHandlerRegistry : Registry
    {
        public BlockingHandlerRegistry()
        {
            For<IHandlerAsync<OrderPlaced>>().Singleton().Use<BlockingOrderProcessor>();
        }
    }
}
