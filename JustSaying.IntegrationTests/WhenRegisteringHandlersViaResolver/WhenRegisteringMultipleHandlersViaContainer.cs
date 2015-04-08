using System;
using System.Linq;
using JustSaying.IntegrationTests.JustSayingFluently;
using NUnit.Framework;
using StructureMap;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class WhenRegisteringMultipleHandlersViaContainer : GivenAPublisher
    {
        private Future<OrderPlaced> _handler1Future;
        private Future<OrderPlaced> _handler2Future;

        protected override void Given()
        {
            ObjectFactory.Initialize(x => x.AddRegistry(new MultipleHandlerRegistry()));

            var handlerResolver = new StructureMapHandlerResolver();

            var handlers = handlerResolver.ResolveHandlers<OrderPlaced>().ToList();
            dynamic handler1 = handlers[0];
            dynamic handler2 = handlers[1];
            _handler1Future = handler1.Future;
            _handler2Future = handler2.Future;

            var subscriber = JustSaying.CreateMeABus.InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            subscriber.StartListening();
        }

        [Test]
        public void FirstHandlerWillReceiveTheMessage()
        {
            Assert.IsTrue(_handler1Future.WaitUntilCompletion(TimeSpan.FromSeconds(20)));
        }

        [Test]
        public void SecondHandlerWillReceiveTheMessage()
        {
            Assert.IsTrue(_handler2Future.WaitUntilCompletion(TimeSpan.FromSeconds(20)));

        }
    }
}