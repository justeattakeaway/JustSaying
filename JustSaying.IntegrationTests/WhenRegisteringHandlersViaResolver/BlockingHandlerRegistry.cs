using JustSaying.IntegrationTests.JustSayingFluently;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using StructureMap.Configuration.DSL;

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