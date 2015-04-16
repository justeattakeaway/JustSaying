using System;
using NUnit.Framework;
using StructureMap;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class WhenRegisteringMultipleHandlersViaContainer : GivenAPublisher
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();

            ObjectFactory.Initialize(x => x.AddRegistry(new MultipleHandlerRegistry()));
        }

        protected override void When()
        {
            var handlerResolver = new StructureMapHandlerResolver();

            var subscriber = JustSaying.CreateMeABus.InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            subscriber.StartListening();
        }

        [Test]
        public void ThrowsNotSupportedException()
        {
            Assert.IsInstanceOf<NotSupportedException>(ThrownException);
        }

        
    }
}