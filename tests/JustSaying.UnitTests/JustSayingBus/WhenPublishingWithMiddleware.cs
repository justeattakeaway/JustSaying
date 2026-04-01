using JustSaying.Messaging;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenPublishingWithMiddleware : GivenAServiceBus
{
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
    private bool _middlewareInvoked;

    protected override async Task WhenAsync()
    {
        SystemUnderTest.AddMessagePublisher<SimpleMessage>(_publisher);
        SystemUnderTest.PublishMiddleware = new TrackingPublishMiddleware(() => _middlewareInvoked = true);

        var cts = new CancellationTokenSource(TimeoutPeriod);
        await SystemUnderTest.StartAsync(cts.Token);

        await SystemUnderTest.PublishAsync(new SimpleMessage());
    }

    [Test]
    public void MiddlewareIsInvoked()
    {
        _middlewareInvoked.ShouldBeTrue();
    }

    [Test]
    public void PublisherIsStillCalled()
    {
        _publisher.Received().PublishAsync(
            Arg.Any<Message>(),
            Arg.Any<PublishMetadata>(),
            Arg.Any<CancellationToken>());
    }

    private class TrackingPublishMiddleware(Action onInvoked) : MiddlewareBase<PublishContext, bool>
    {
        protected override async Task<bool> RunInnerAsync(
            PublishContext context,
            Func<CancellationToken, Task<bool>> func,
            CancellationToken stoppingToken)
        {
            onInvoked();
            return await func(stoppingToken).ConfigureAwait(false);
        }
    }
}
