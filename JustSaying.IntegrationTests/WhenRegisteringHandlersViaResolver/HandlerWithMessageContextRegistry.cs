using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using StructureMap;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class HandlerWithMessageContextRegistry : Registry
    {
        public HandlerWithMessageContextRegistry(RecordingMessageContextAccessor accessor)
        {
            For<IHandlerAsync<OrderPlaced>>().Use<HandlerWithMessageContext>()
                .Ctor<Future<OrderPlaced>>().Is(new Future<OrderPlaced>());

            For<IMessageContextAccessor>().Use(accessor);
            For<IMessageContextReader>().Use(accessor);
        }
    }
}
