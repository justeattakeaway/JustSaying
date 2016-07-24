using System.Threading.Tasks;
using JustBehave;
using JustSaying.IntegrationTests.TestHandlers;
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

            CreateMeABus.InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueueNamed("container-test")
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