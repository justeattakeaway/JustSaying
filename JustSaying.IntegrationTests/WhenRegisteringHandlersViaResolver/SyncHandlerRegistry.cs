using JustSaying.IntegrationTests.JustSayingFluently;
using JustSaying.Messaging.MessageHandling;
using StructureMap.Configuration.DSL;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class SyncHandlerRegistry : Registry
    {
        public SyncHandlerRegistry()
        {
#pragma warning disable 618
            For<IHandler<OrderPlaced>>().Transient().Use<SyncOrderProcessor>()
                .Ctor<Future<OrderPlaced>>().Is(new Future<OrderPlaced>());
#pragma warning restore 618
        }
    }
}