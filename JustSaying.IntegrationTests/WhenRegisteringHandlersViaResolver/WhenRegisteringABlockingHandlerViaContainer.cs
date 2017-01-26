using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;
using StructureMap;

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
            Assert.That(handler, Is.Not.Null);

            var blockingHandler = (BlockingHandler<OrderPlaced>)handler;
            _resolvedHandler = (BlockingOrderProcessor)blockingHandler.Inner;
            DoneSignal = _resolvedHandler.DoneSignal.Task;

            Subscriber = CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            Subscriber.StartListening();
        }

        [Test]
        public void ThenHandlerWillReceiveTheMessage()
        {
            _resolvedHandler.ReceivedMessageCount.ShouldBeGreaterThan(0);
        }
    }
}
