using System.Linq;
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

           var handlerResolver = new StructureMapHandlerResolver(container);
            var handlers = handlerResolver.ResolveHandlers<OrderPlaced>().ToList();
            Assert.That(handlers.Count, Is.EqualTo(1));

            _resolvedHandler = (BlockingOrderProcessor)handlers[0];
            DoneSignal = _resolvedHandler.DoneSignal.Task;

            var subscriber = CreateMeABus.InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            subscriber.StartListening();
        }

        [Test]
        public void ThenHandlerWillReceiveTheMessage()
        {
            _resolvedHandler.ReceivedMessageCount.ShouldBeGreaterThan(0);
        }
    }
}