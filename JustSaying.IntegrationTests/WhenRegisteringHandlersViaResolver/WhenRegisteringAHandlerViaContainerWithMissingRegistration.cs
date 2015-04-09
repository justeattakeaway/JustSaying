using JustBehave;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class WhenRegisteringAHandlerViaContainerWithMissingRegistration : GivenAPublisher
    {
        protected override void Given()
        {
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            base.When();
            var handlerResolver = new StructureMapHandlerResolver();

            var subscriber = JustSaying.CreateMeABus.InRegion("eu-west-1")
                .WithSqsTopicSubscriber()
                .IntoQueue("container-test")
                .WithMessageHandler<OrderPlaced>(handlerResolver);

            subscriber.StartListening();
        }

        [Then]
        public void ExceptionIsThrownBecauseHandlerIsNotRegisteredInContainer()
        {
            Assert.IsInstanceOf<HandlerNotRegisteredWithContainerException>(ThrownException);
        }
    }
}