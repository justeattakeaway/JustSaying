using System;
using JustBehave;
using JustSaying.AwsTools;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using JustSaying.Models;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenPublishingFails : GivenAServiceBus
    {
        private readonly IPublisher _publisher = Substitute.For<IPublisher>();
        private const int PublishAttempts = 2;

        protected override void Given()
        {
            base.Given();
            Config.PublishFailureReAttempts.Returns(PublishAttempts);
            Config.PublishFailureBackoffMilliseconds.Returns(0);
            RecordAnyExceptionsThrown();
            _publisher.When(x => x.Publish(Arg.Any<string>(), Arg.Any<string>())).Do(x => { throw new Exception(); });
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<GenericMessage>(_publisher, string.Empty);

            SystemUnderTest.Publish(new GenericMessage());
        }

        [Then]
        public void EventPublicationWasAttemptedTheConfiguredNumberOfTimes()
        {
            Patiently.VerifyExpectation(() => _publisher.Received(PublishAttempts).Publish(Arg.Any<string>(), Arg.Any<string>()));
        }
    }
}