using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;
using Shouldly;
using StructureMap;
using Xunit;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenRegisteringAHandlerViaContainerWithMissingRegistration : GivenAPublisher
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override Task When()
        {
            var container = new Container((p) => p.For<IHandlerAsync<OrderPlaced>>().Use<OrderPlacedHandler>());

            var handlerResolver = new StructureMapHandlerResolver(container);

            CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            return Task.FromResult(true);
        }

        [Fact]
        public void ExceptionIsThrownBecauseHandlerIsNotRegisteredInContainer()
        {
            ThrownException.ShouldBeAssignableTo<HandlerNotRegisteredWithContainerException>();
        }
    }
}
