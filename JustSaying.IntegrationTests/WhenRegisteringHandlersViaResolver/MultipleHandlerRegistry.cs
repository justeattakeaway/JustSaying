using JustSaying.IntegrationTests.JustSayingFluently;
using JustSaying.Messaging.MessageHandling;
using StructureMap.Configuration.DSL;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class MultipleHandlerRegistry : Registry
    {
        public MultipleHandlerRegistry()
        {
            For<IAsyncHandler<OrderPlaced>>().Transient().Use<OrderProcessor>()
                .Ctor<Future<OrderPlaced>>().Is(new Future<OrderPlaced>());
            
            For<IAsyncHandler<OrderPlaced>>().Transient().Use<OrderDispatcher>()
                .Ctor<Future<OrderPlaced>>().Is(new Future<OrderPlaced>());
        }
    }
}