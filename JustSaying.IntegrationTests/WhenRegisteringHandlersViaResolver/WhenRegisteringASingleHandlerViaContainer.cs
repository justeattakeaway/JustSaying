using System;
using JustSaying.IntegrationTests.JustSayingFluently;
using NUnit.Framework;
using StructureMap;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class WhenRegisteringASingleHandlerViaContainer : GivenAPublisher
    {
        private Future<OrderPlaced> _handlerFuture;

        protected override void Given()
        {
            ObjectFactory.Initialize(x => x.AddRegistry(new SingleHandlerRegistry()));

            var handlerResolver = new StructureMapHandlerResolver();

            _handlerFuture = ((OrderProcessor)handlerResolver.ResolveHandlers<OrderPlaced>()).Future;

            var subscriber = JustSaying.CreateMeABus.InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            subscriber.StartListening();
        }

        [Test]
        public void ThenHandlerWillReceiveTheMessage()
        {
            Assert.IsTrue(_handlerFuture.WaitUntilCompletion(TimeSpan.FromSeconds(20)));
        }
    }
}