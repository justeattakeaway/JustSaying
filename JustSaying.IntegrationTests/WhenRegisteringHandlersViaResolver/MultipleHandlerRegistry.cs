using JustSaying.IntegrationTests.JustSayingFluently;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using StructureMap.Configuration.DSL;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class MultipleHandlerRegistry : Registry
    {
        public MultipleHandlerRegistry()
        {
            For<IHandlerAsync<OrderPlaced>>().Transient().Use<OrderProcessor>()
                .Ctor<Future<OrderPlaced>>().Is(new Future<OrderPlaced>());
            
            For<IHandlerAsync<OrderPlaced>>().Transient().Use<OrderDispatcher>()
                .Ctor<Future<OrderPlaced>>().Is(new Future<OrderPlaced>());
        }
    }
}