using System;
using System.Threading;
using NUnit.Framework;
using StructureMap;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;
using Container = StructureMap.Container;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class WhenRegisteringMultipleHandlersViaContainer : GivenAPublisher
    {
        private IContainer _container;
        private IHandlerResolver handlerResolver;

        protected override void Given()
        {
            RecordAnyExceptionsThrown();

            _container = new Container(x => x.AddRegistry(new MultipleHandlerRegistry()));
        }

        protected override Task When()
        {
            handlerResolver = new StructureMapHandlerResolver(_container);

            CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            return Task.FromResult(true);
        }

        [Test]
        public void WillResolveIfContainerCanResolveSingleInstance()
        {
            //Note Structuremap registry will overwrite the OrderProcessor with OrderDispatcher
            var resolvedHandler = handlerResolver.ResolveHandler<OrderPlaced>(new HandlerResolutionContext("container-test"));
            Assert.IsInstanceOf<OrderDispatcher>(resolvedHandler);
        }
    }
}
