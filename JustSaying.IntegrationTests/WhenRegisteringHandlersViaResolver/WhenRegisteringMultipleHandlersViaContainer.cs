using System;
using NUnit.Framework;
using StructureMap;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using Container = StructureMap.Container;

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

        protected override Task When()
        {
            var handlerResolver = new StructureMapHandlerResolver(_container);

            CreateMeABus.InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueueNamed("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            return Task.FromResult(true);
        }

        [Test]
        public void ThrowsNotSupportedException()
        {
            Assert.IsInstanceOf<NotSupportedException>(ThrownException);
        }
    }
}