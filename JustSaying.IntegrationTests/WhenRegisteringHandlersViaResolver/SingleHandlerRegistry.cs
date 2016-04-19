using JustSaying.IntegrationTests.JustSayingFluently;
using JustSaying.Messaging.MessageHandling;
using StructureMap.Configuration.DSL;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class SingleHandlerRegistry : Registry
    {
        public SingleHandlerRegistry()
        {
            For<IAsyncHandler<OrderPlaced>>().Transient().Use<OrderProcessor>()
                .Ctor<Future<OrderPlaced>>().Is(new Future<OrderPlaced>());
            
        }
    }
}