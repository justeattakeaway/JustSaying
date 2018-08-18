using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
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

            var fixture = new JustSayingFixture();

            fixture.Builder()
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            return Task.FromResult(true);
        }

        [AwsFact]
        public void ExceptionIsThrownBecauseHandlerIsNotRegisteredInContainer()
        {
            ThrownException.ShouldBeAssignableTo<HandlerNotRegisteredWithContainerException>();
        }
    }
}
