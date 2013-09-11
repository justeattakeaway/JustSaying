using System;
using System.Threading;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Messaging.Messages.OrderDispatch;
using JustEat.Testing;
using NSubstitute;

namespace Stack.UnitTests.NotificationStack
{
    public class WhenPublishingFails : NotificationStackBaseTest
    {
        private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
        private const int PublishAttempts = 4;

        protected override void Given()
        {
            Config.PublishFailureReAttempts.Returns(4);
            Config.PublishFailureBackoffMilliseconds.Returns(1);
            RecordAnyExceptionsThrown();
            _publisher.When(x => x.Publish(Arg.Any<Message>())).Do(x => { throw new Exception(); });
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<OrderAccepted>("OrderDispatch", _publisher);

            SystemUnderTest.Publish(new OrderAccepted(0, 0, 0));
            Thread.Sleep(20);
        }

        [Then]
        public void EventPublicationWasAttemptedTheConfiguredNumberOfTimes()
        {
            _publisher.Received(PublishAttempts).Publish(Arg.Any<OrderAccepted>());
        }
    }
}