using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using JustSaying.Models;
using NSubstitute;
using Xunit;

namespace JustSaying.UnitTests.JustSayingBus
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

            _publisher.When(x => x.PublishAsync(Arg.Any<Message>()))
                .Do(x => { throw new TestException("Thrown by test WhenPublishingFails"); });
        }

        protected override async Task When()
        {
            SystemUnderTest.AddMessagePublisher<GenericMessage>(_publisher, string.Empty);

            await SystemUnderTest.PublishAsync(new GenericMessage());
        }

        [Fact]
        public void EventPublicationWasAttemptedTheConfiguredNumberOfTimes()
        {
            _publisher
                .Received(PublishAttempts)
                .PublishAsync(Arg.Any<GenericMessage>());
        }
    }
}
