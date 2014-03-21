using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Testing;
using NSubstitute;
using SimpleMessageMule.TestingFramework;
using Tests.MessageStubs;

namespace SimpleMessageMule.UnitTests.NotificationStack
{
    public class WhenPublishingMessages : NotificationStackBaseTest
    {
        private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();

        protected override void Given()
        {
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<GenericMessage>("OrderDispatch", _publisher);

            SystemUnderTest.Publish(new GenericMessage());
        }

        [Then]
        public void PublisherIsCalledToPublish()
        {
            _publisher.Received().Publish(Arg.Any<GenericMessage>());
        }

        [Then]
        public void PublishMessageTimeStatsSent()
        {
            Patiently.VerifyExpectation(() => Monitor.Received(1).PublishMessageTime(Arg.Any<long>()), 10.Seconds());
        }
    }
}