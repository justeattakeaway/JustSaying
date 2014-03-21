using System.Threading;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Testing;
using NSubstitute;
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
            Thread.Sleep(20);
        }

        [Then]
        public void PublisherIsCalledToPublish()
        {
            _publisher.Received().Publish(Arg.Any<GenericMessage>());
        }

        [Then]
        public void PublishMessageTimeStatsSent()
        {
            Monitor.Received(1).PublishMessageTime(Arg.Any<long>());
        }
    }
}