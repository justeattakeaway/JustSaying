using System.Linq;
using JustSaying.IntegrationTests.JustSayingFluently;
using JustSaying.Messaging.MessageHandling;
using NUnit.Framework;
using Shouldly;
using StructureMap;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class WhenRegisteringABlockingHandlerViaContainer : GivenAPublisher
    {
        private Future<OrderPlaced> _handlerFuture;

        protected override void Given()
        {
           var container = new Container(x => x.AddRegistry(new BlockingHandlerRegistry()));

           var handlerResolver = new StructureMapHandlerResolver(container);
            var handlers = handlerResolver.ResolveHandlers<OrderPlaced>().ToList();
            Assert.That(handlers.Count, Is.EqualTo(1));

            var resolvedHandler = (BlockingHandler<OrderPlaced>)handlers[0];
            _handlerFuture = ((BlockingOrderProcessor)resolvedHandler.Inner).Future;
            DoneSignal = _handlerFuture.DoneSignal;

            var subscriber = CreateMeABus.InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            subscriber.StartListening();
        }

        [Test]
        public void ThenHandlerWillReceiveTheMessage()
        {
            _handlerFuture.ReceivedMessageCount.ShouldBeGreaterThan(0);
        }
    }
}