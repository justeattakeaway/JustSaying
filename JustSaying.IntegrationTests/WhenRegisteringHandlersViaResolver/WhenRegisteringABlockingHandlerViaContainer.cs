using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;

using Shouldly;
using StructureMap;
using Xunit;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class WhenRegisteringABlockingHandlerViaContainer : GivenAPublisher
    {
        private BlockingOrderProcessor _resolvedHandler;

        protected override void Given()
        {
           var container = new Container(x => x.AddRegistry(new BlockingHandlerRegistry()));
            var resolutionContext = new HandlerResolutionContext("test");

           var handlerResolver = new StructureMapHandlerResolver(container);
            var handler = handlerResolver.ResolveHandler<OrderPlaced>(resolutionContext);
            handler.ShouldNotBeNull();

            // we use the obsolete interface"IHandler<T>" here
#pragma warning disable 618
            var blockingHandler = (BlockingHandler<OrderPlaced>)handler;
            _resolvedHandler = (BlockingOrderProcessor)blockingHandler.Inner;
#pragma warning restore 618

            DoneSignal = _resolvedHandler.DoneSignal.Task;

            Subscriber = CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            Subscriber.StartListening();
        }

        [Fact]
        public void ThenHandlerWillReceiveTheMessage()
        {
            _resolvedHandler.ReceivedMessageCount.ShouldBeGreaterThan(0);
        }
    }
}
