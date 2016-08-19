using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using StructureMap;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class SingleHandlerRegistry : Registry
    {
        public SingleHandlerRegistry()
        {
            For<IHandlerAsync<OrderPlaced>>().Singleton().Use<OrderProcessor>()
                .Ctor<Future<OrderPlaced>>().Is(new Future<OrderPlaced>());
        }
    }
}