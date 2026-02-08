using JustSaying.Messaging;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenPublishingWithPerTypeMiddleware(ITestOutputHelper outputHelper) : GivenAServiceBus(outputHelper)
{
    private readonly IMessagePublisher _simplePublisher = Substitute.For<IMessagePublisher>();
    private readonly IMessagePublisher _anotherPublisher = Substitute.For<IMessagePublisher>();
    private bool _perTypeMiddlewareInvoked;
    private bool _globalMiddlewareInvoked;

    protected override async Task WhenAsync()
    {
        SystemUnderTest.AddMessagePublisher<SimpleMessage>(_simplePublisher);
        SystemUnderTest.AddMessagePublisher<AnotherSimpleMessage>(_anotherPublisher);

        // Global middleware (fallback)
        SystemUnderTest.PublishMiddleware = new TrackingMiddleware(() => _globalMiddlewareInvoked = true);

        // Per-type middleware for SimpleMessage only
        SystemUnderTest.AddPublishMiddleware<SimpleMessage>(
            new TrackingMiddleware(() => _perTypeMiddlewareInvoked = true));

        var cts = new CancellationTokenSource(TimeoutPeriod);
        await SystemUnderTest.StartAsync(cts.Token);

        await SystemUnderTest.PublishAsync(new SimpleMessage());
        await SystemUnderTest.PublishAsync(new AnotherSimpleMessage());
    }

    [Fact]
    public void PerTypeMiddlewareIsInvokedForConfiguredType()
    {
        _perTypeMiddlewareInvoked.ShouldBeTrue();
    }

    [Fact]
    public void GlobalMiddlewareIsUsedAsFallback()
    {
        _globalMiddlewareInvoked.ShouldBeTrue();
    }

    [Fact]
    public void BothPublishersAreCalled()
    {
        _simplePublisher.Received().PublishAsync(
            Arg.Any<Message>(),
            Arg.Any<PublishMetadata>(),
            Arg.Any<CancellationToken>());

        _anotherPublisher.Received().PublishAsync(
            Arg.Any<Message>(),
            Arg.Any<PublishMetadata>(),
            Arg.Any<CancellationToken>());
    }

    private class TrackingMiddleware(Action onInvoked) : MiddlewareBase<PublishContext, bool>
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
