using System.Linq;
using JustSaying.IntegrationTests.JustSayingFluently;
using JustSaying.IntegrationTests.TestHandlers;
using NUnit.Framework;
using Shouldly;
using StructureMap;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class WhenRegisteringASingleHandlerViaContainer : GivenAPublisher
    {
        private Future<OrderPlaced> _handlerFuture;

        protected override void Given()
        {
           var container = new Container(x => x.AddRegistry(new SingleHandlerRegistry()));

           var handlerResolver = new StructureMapHandlerResolver(container);
            var handler = handlerResolver.ResolveHandler<OrderPlaced>();
            Assert.That(handler, Is.Not.Null);

            _handlerFuture = ((OrderProcessor)handler).Future;
            DoneSignal = _handlerFuture.DoneSignal;

            Subscriber = CreateMeABus.InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            Subscriber.StartListening();
        }

        [Test]
        public void ThenHandlerWillReceiveTheMessage()
        {
            _handlerFuture.ReceivedMessageCount.ShouldBeGreaterThan(0);
        }
    }
}