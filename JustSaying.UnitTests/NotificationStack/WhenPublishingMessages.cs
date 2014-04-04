using JustSaying.Messaging;
using JustEat.Testing;
using JustSaying.Tests.MessageStubs;
using NSubstitute;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.NotificationStack
{
    public class WhenPublishingMessages : GivenAServiceBus
    {
        private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
        
        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<GenericMessage>("OrderDispatch", _publisher);

            SystemUnderTest.Publish(new GenericMessage());
        }

        [Then]
        public void PublisherIsCalledToPublish()
        {
            Patiently.VerifyExpectation(() => _publisher.Received().Publish(Arg.Any<GenericMessage>()));
        }

        [Then]
        public void PublishMessageTimeStatsSent()
        {
            //todo: failing for the right reason. Must make sure Maxim's recent commit is merged.
            Patiently.VerifyExpectation(() => Monitor.Received(1).PublishMessageTime(Arg.Any<long>()), 10.Seconds());
        }
    }
}