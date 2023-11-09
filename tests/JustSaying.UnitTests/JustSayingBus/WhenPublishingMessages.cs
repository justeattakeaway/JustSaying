using JustSaying.Messaging;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenPublishingMessages(ITestOutputHelper outputHelper) : GivenAServiceBus(outputHelper)
{
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();

    protected override async Task WhenAsync()
    {
        SystemUnderTest.AddMessagePublisher<SimpleMessage>(_publisher);

        var cts = new CancellationTokenSource(TimeoutPeriod);
        await SystemUnderTest.StartAsync(cts.Token);

        await SystemUnderTest.PublishAsync(new SimpleMessage());
    }

    [Fact]
    public void PublisherIsCalledToPublish()
    {
        _publisher.Received().PublishAsync(Arg.Any<Message>(),
            Arg.Any<PublishMetadata>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public void PublishMessageTimeStatsSent()
    {
        Monitor.PublishMessageTimes.ShouldHaveSingleItem();
    }
}