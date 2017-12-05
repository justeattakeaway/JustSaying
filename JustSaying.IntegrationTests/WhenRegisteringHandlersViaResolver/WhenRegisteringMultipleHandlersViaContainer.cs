using System;
using StructureMap;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;
using Container = StructureMap.Container;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    [Collection(GlobalSetup.CollectionName)]
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

            CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            return Task.FromResult(true);
        }

        [Fact]
        public void ThrowsNotSupportedException()
        {
            ThrownException.ShouldBeAssignableTo<NotSupportedException>();
        }
    }
}
