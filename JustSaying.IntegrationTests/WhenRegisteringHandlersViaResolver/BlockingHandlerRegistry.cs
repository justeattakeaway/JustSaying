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
            For<IHandler<OrderPlaced>>().Transient().Use<BlockingOrderProcessor>()
                .Ctor<Future<OrderPlaced>>().Is(new Future<OrderPlaced>());
#pragma warning restore 618
        }
    }
}