using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenRegisteringPublishers(ITestOutputHelper outputHelper) : GivenAServiceBus(outputHelper)
{
    private IMessagePublisher _publisher;

    protected override void Given()
    {
        base.Given();
        _publisher = Substitute.For<IMessagePublisher>();
    }

    protected override async Task WhenAsync()
    {
        SystemUnderTest.AddMessagePublisher<OrderAccepted>(_publisher);
        SystemUnderTest.AddMessagePublisher<OrderRejected>(_publisher);

        var cts = new CancellationTokenSource(TimeoutPeriod);
        await SystemUnderTest.StartAsync(cts.Token);

        await SystemUnderTest.PublishAsync(new OrderAccepted());
        await SystemUnderTest.PublishAsync(new OrderRejected());
        await SystemUnderTest.PublishAsync(new OrderRejected());
    }

    [Fact]
    public void AcceptedOrderWasPublishedOnce()
    {
        _publisher.Received(1).PublishAsync(
            Arg.Is<Message>(m => m is OrderAccepted),
            Arg.Any<PublishMetadata>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RejectedOrderWasPublishedTwice()
    {
        _publisher.Received(2).PublishAsync(
            Arg.Is<Message>(m => m is OrderRejected),
            Arg.Any<PublishMetadata>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AndInterrogationShowsPublishersHaveBeenSet()
    {
        dynamic response = SystemUnderTest.Interrogate();

        Dictionary<string, InterrogationResult> publishedTypes = response.Data.PublishedMessageTypes;

        publishedTypes.ShouldContainKey(nameof(OrderAccepted));
        publishedTypes.ShouldContainKey(nameof(OrderRejected));
    }
}
