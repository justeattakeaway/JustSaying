using System.Threading.Tasks;
using JustBehave;
using JustSaying.IntegrationTests.TestHandlers;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using StructureMap;

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

        [Then]
        public void ExceptionIsThrownBecauseHandlerIsNotRegisteredInContainer()
        {
            Assert.IsInstanceOf<HandlerNotRegisteredWithContainerException>(ThrownException);
        }
    }
}
