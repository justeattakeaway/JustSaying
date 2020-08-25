using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Models;
using JustSaying.TestingFramework;
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
            Config.PublishFailureBackoff.Returns(TimeSpan.Zero);
            RecordAnyExceptionsThrown();

            _publisher.When(x => x.PublishAsync(Arg.Any<Message>(),
                    Arg.Any<PublishMetadata>(), Arg.Any<CancellationToken>()))
                .Do(x => { throw new TestException("Thrown by test WhenPublishingFails"); });
        }

        protected override async Task WhenAsync()
        {
            SystemUnderTest.AddMessagePublisher<SimpleMessage>(_publisher, string.Empty);

           await SystemUnderTest.StartAsync(CancellationToken.None);

            await SystemUnderTest.PublishAsync(new SimpleMessage());
        }

        [Fact]
        public void EventPublicationWasAttemptedTheConfiguredNumberOfTimes()
        {
            _publisher
                .Received(PublishAttempts)
                .PublishAsync(Arg.Any<Message>(), Arg.Any<PublishMetadata>(), CancellationToken.None);
        }
    }
}
