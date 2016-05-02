using JustSaying.IntegrationTests.JustSayingFluently;
using JustSaying.Messaging.MessageHandling;
using StructureMap.Configuration.DSL;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class BlockingHandlerRegistry : Registry
    {
        public BlockingHandlerRegistry()
        {
#pragma warning disable 618
            For<IHandler<OrderPlaced>>().Singleton().Use<BlockingOrderProcessor>();
#pragma warning restore 618
        }
    }
}