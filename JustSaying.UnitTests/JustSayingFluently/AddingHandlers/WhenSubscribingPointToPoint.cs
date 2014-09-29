using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.UnitTests.JustSayingFluently.AddingHandlers
{
    public class WhenSubscribingPointToPoint : JustSayingFluentlyTestBase
    {
        private readonly IHandler<Message> _handler = Substitute.For<IHandler<Message>>();
        private object _response;

        protected override void Given(){}

        protected override void When()
        {
            _response = SystemUnderTest.WithSqsPointToPointSubscriber().IntoQueue("queuename").ConfigureSubscriptionWith(
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

        [Then]
        public void NoTopicIsCreated()
        {
            QueueVerifier.DidNotReceiveWithAnyArgs().EnsureTopicExistsWithQueueSubscribed(null, null, null);
        }

        [Then]
        public void TheQueueIsCreated()
        {
            QueueVerifier.Received().EnsureQueueExists(Configuration.Region, Arg.Any<SqsReadConfiguration>());
        }
    }
}