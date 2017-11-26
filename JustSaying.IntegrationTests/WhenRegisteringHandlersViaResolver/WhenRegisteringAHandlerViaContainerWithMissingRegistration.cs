using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using Microsoft.Extensions.Logging;
using Shouldly;
using StructureMap;
using Xunit;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class WhenRegisteringAHandlerViaContainerWithMissingRegistration : GivenAPublisher
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override Task When()
        {
            var handlerResolver = new StructureMapHandlerResolver(new Container());

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
