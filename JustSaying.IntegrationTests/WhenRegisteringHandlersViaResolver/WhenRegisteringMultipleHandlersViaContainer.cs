using System;
using NUnit.Framework;
using StructureMap;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class WhenRegisteringMultipleHandlersViaContainer : GivenAPublisher
    {
        private IContainer _container;

        protected override void Given()
        {
            RecordAnyExceptionsThrown();

            _container = new Container(x => x.AddRegistry(new MultipleHandlerRegistry()));
        }

        protected override void When()
        {
            var handlerResolver = new StructureMapHandlerResolver(_container);

            CreateMeABus.InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);
        }

        [Test]
        public void ThrowsNotSupportedException()
        {
            Assert.IsInstanceOf<NotSupportedException>(ThrownException);
        }
    }
}