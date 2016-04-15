using System.Linq;
using JustSaying.IntegrationTests.JustSayingFluently;
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

            _handlerFuture = ((OrderProcessor)handlerResolver.ResolveHandlers<OrderPlaced>().Single()).Future;

            var subscriber = CreateMeABus.InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            subscriber.StartListening();
        }

        [Test]
        public void ThenHandlerWillReceiveTheMessage()
        {
            _handlerFuture.MessageCount.ShouldBeGreaterThan(0);
        }
    }
}