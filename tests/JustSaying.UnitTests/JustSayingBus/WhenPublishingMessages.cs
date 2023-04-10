using JustSaying.Messaging;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenPublishingMessages : GivenAServiceBus
{
    private readonly IMessagePublisher<SimpleMessage> _publisher = Substitute.For<IMessagePublisher<SimpleMessage>>();

    protected override async Task WhenAsync()
    {
        SystemUnderTest.AddMessagePublisher(_publisher);

        var cts = new CancellationTokenSource(TimeoutPeriod);
        await SystemUnderTest.StartAsync(cts.Token);

        await SystemUnderTest.PublishAsync(new SimpleMessage());
    }

    [Fact]
    public void PublisherIsCalledToPublish()
    {
        _publisher.Received().PublishAsync(Arg.Any<SimpleMessage>(),
            Arg.Any<PublishMetadata>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public void PublishMessageTimeStatsSent()
    {
        Monitor.PublishMessageTimes.ShouldHaveSingleItem();
    }

    public WhenPublishingMessages(ITestOutputHelper outputHelper) : base(outputHelper)
    { }
}
