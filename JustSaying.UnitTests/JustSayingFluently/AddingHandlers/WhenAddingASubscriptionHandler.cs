using JustEat.Testing;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingFluently.AddingHandlers
{
    public class WhenAddingASubscriptionHandler : JustSayingFluentlyTestBase
    {
        private readonly IHandler<Message> _handler = Substitute.For<IHandler<Message>>();
        private object _response;

        protected override void Given(){}

        protected override void When()
        {
            _response = SystemUnderTest.WithSqsTopicSubscriber().IntoQueue("queuename").ConfigureSubscriptionWith(
                cfg =>
                {
                    cfg.MessageRetentionSeconds = 60;
                }).WithMessageHandler(_handler);
        }

        [Then]
        public void HandlerIsAddedToBus()
        {
            NotificationStack.Received().AddMessageHandler(_handler);
        }
        
        [Then]
        public void SerialisationIsRegisteredForMessage()
        {
            NotificationStack.SerialisationRegister.Received().AddSerialiser<Message>(Arg.Any<IMessageSerialiser<Message>>());
        }

        [Then]
        public void ICanContinueConfiguringTheBus()
        {
            Assert.IsInstanceOf<IFluentSubscription>(_response);
        }
    }
}
