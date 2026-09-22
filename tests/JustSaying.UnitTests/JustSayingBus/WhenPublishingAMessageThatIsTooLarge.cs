using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

/// <summary>
/// A message that is too large will be too large on every attempt, so retrying only delays the failure.
/// </summary>
public class WhenPublishingAMessageThatIsTooLarge : GivenAServiceBus
{
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();

    protected override void Given()
    {
        base.Given();

        Config.PublishFailureReAttempts.Returns(3);
        Config.PublishFailureBackoff.Returns(TimeSpan.Zero);
        RecordAnyExceptionsThrown();

        _publisher.When(x => x.PublishAsync(Arg.Any<Message>(),
                Arg.Any<PublishMetadata>(),
                Arg.Any<CancellationToken>()))
            .Do(x => { throw new MessageTooLargeException("Thrown by test"); });
    }

    protected override async Task WhenAsync()
    {
        SystemUnderTest.AddMessagePublisher<SimpleMessage>(_publisher);

        var cts = new CancellationTokenSource(TimeoutPeriod);
        await SystemUnderTest.StartAsync(cts.Token);

        await SystemUnderTest.PublishAsync(new SimpleMessage(), cts.Token);
    }

    [Test]
    public void TheExceptionIsThrown()
    {
        ThrownException.ShouldBeOfType<MessageTooLargeException>();
    }

    [Test]
    public void ThePublishIsNotRetried()
    {
        _publisher
            .Received(1)
            .PublishAsync(Arg.Any<Message>(), Arg.Any<PublishMetadata>(), Arg.Any<CancellationToken>());
    }
}
