using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using StructureMap;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class HandlerWithMessageContextRegistry : Registry
    {
        public HandlerWithMessageContextRegistry()
        {
            For<IHandlerAsync<OrderPlaced>>().Use<HandlerWithMessageContext>()
                .Ctor<Future<OrderPlaced>>().Is(new Future<OrderPlaced>());
            For<IMessageContextReader>().Use<MessageContextAccessor>();
        }
    }
}
