using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
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

        protected override void Given()
        {
        }

        protected override void When()
        {
            _response = SystemUnderTest
                .WithSqsTopicSubscriber()
                .IntoQueue(string.Empty)
                .ConfigureSubscriptionWith(cfg => { })
                .WithMessageHandler(_handler);
        }

        [Then]
        public void TheTopicAndQueueIsCreatedInEachRegion()
        {
            QueueVerifier.Received().EnsureTopicExistsWithQueueSubscribed("defaultRegion", Bus.SerialisationRegister, Arg.Any<SqsReadConfiguration>());
            QueueVerifier.Received().EnsureTopicExistsWithQueueSubscribed("failoverRegion", Bus.SerialisationRegister, Arg.Any<SqsReadConfiguration>());
        }

        [Then]
        public void TheSubscriptionIsCreatedInEachRegion()
        {
            Bus.Received(2).AddNotificationTopicSubscriber(Arg.Any<string>(), Arg.Any<INotificationSubscriber>());
        }

        [Then]
        public void HandlerIsAddedToBus()
        {
            Bus.Received().AddMessageHandler(_handler);
        }

        [Then]
        public void SerialisationIsRegisteredForMessage()
        {
            Bus.SerialisationRegister.Received().AddSerialiser<Message>(Arg.Any<IMessageSerialiser<Message>>());
        }

        [Then]
        public void ICanContinueConfiguringTheBus()
        {
            Assert.IsInstanceOf<IFluentSubscription>(_response);
        }
    }
}
