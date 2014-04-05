using System;
using System.Threading;
using JustSaying.Messaging;
using JustSaying.Messaging.Messages;
using JustEat.Testing;
using JustSaying.Tests.MessageStubs;
using NSubstitute;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.NotificationStack
{
    public class WhenPublishingFails : GivenAServiceBus
    {
        private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
        private const int PublishAttempts = 2;

        protected override void Given()
        {
            base.Given();
            Config.PublishFailureReAttempts.Returns(PublishAttempts);
            Config.PublishFailureBackoffMilliseconds.Returns(0);
            RecordAnyExceptionsThrown();
            _publisher.When(x => x.Publish(Arg.Any<Message>())).Do(x => { throw new Exception(); });
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<GenericMessage>("OrderDispatch", _publisher);

            SystemUnderTest.Publish(new GenericMessage());
        }

        [Then]
        public void EventPublicationWasAttemptedTheConfiguredNumberOfTimes()
        {
            Patiently.VerifyExpectation(() => _publisher.Received(PublishAttempts).Publish(Arg.Any<GenericMessage>()));
        }
    }
}