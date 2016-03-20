using System;
using System.Threading.Tasks;
using JustBehave;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using JustSaying.Models;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenPublishingFails : GivenAServiceBus
    {
        private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
        private const int PublishAttempts = 2;

        protected override void Given()
        {
            Logging.ToConsole();
            base.Given();

            Config.PublishFailureReAttempts.Returns(PublishAttempts);
            Config.PublishFailureBackoffMilliseconds.Returns(0);
            RecordAnyExceptionsThrown();

            _publisher.When(x => x.Publish(Arg.Any<Message>()))
                .Do(x => { throw new TestException("Thrown by test WhenPublishingFails"); });
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<GenericMessage>(_publisher, string.Empty);

            SystemUnderTest.Publish(new GenericMessage());
        }

        [Then]
        public async Task EventPublicationWasAttemptedTheConfiguredNumberOfTimes()
        {
            await Patiently.VerifyExpectationAsync(() => 
                _publisher
                    .Received(PublishAttempts)
                    .Publish(Arg.Any<GenericMessage>()));
        }
    }
}