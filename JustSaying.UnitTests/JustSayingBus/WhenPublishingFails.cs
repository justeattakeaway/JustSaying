using System;
using System.Threading;
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

        protected override async Task Given()
        {
            await base.Given();

            Config.PublishFailureReAttempts.Returns(PublishAttempts);
            Config.PublishFailureBackoff.Returns(TimeSpan.Zero);
            RecordAnyExceptionsThrown();

            _publisher.When(x => x.PublishAsync(Arg.Any<PublishEnvelope>(), Arg.Any<CancellationToken>()))
                .Do(x => { throw new TestException("Thrown by test WhenPublishingFails"); });
        }

        protected override async Task When()
        {
            SystemUnderTest.AddMessagePublisher<SimpleMessage>(_publisher, string.Empty);

            await SystemUnderTest.PublishAsync(new SimpleMessage());
        }

        [Fact]
        public void EventPublicationWasAttemptedTheConfiguredNumberOfTimes()
        {
            _publisher
                .Received(PublishAttempts)
                .PublishAsync(Arg.Any<PublishEnvelope>(), CancellationToken.None);
        }
    }
}
