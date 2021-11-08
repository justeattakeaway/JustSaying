using JustSaying.Messaging;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

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
                Arg.Any<PublishMetadata>(),
                Arg.Any<CancellationToken>()))
            .Do(x => { throw new TestException("Thrown by test WhenPublishingFails"); });
    }

    protected override async Task WhenAsync()
    {
        SystemUnderTest.AddMessagePublisher<SimpleMessage>(_publisher);

        var cts = new CancellationTokenSource(TimeoutPeriod);
        await SystemUnderTest.StartAsync(cts.Token);

        await SystemUnderTest.PublishAsync(new SimpleMessage(), cts.Token);
    }

    [Fact]
    public void EventPublicationWasAttemptedTheConfiguredNumberOfTimes()
    {
        _publisher
            .Received(PublishAttempts)
            .PublishAsync(Arg.Any<Message>(), Arg.Any<PublishMetadata>(), Arg.Any<CancellationToken>());
    }

    public WhenPublishingFails(ITestOutputHelper outputHelper) : base(outputHelper)
    { }
}