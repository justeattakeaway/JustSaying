using JustSaying.Messaging;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenBatchPublishingWithPerTypeMiddleware(ITestOutputHelper outputHelper) : GivenAServiceBus(outputHelper)
{
    private readonly IMessageBatchPublisher _simpleBatchPublisher = Substitute.For<IMessageBatchPublisher, IMessagePublisher>();
    private readonly IMessageBatchPublisher _anotherBatchPublisher = Substitute.For<IMessageBatchPublisher, IMessagePublisher>();
    private bool _perTypeMiddlewareInvoked;
    private bool _globalMiddlewareInvoked;

    protected override async Task WhenAsync()
    {
        SystemUnderTest.AddMessageBatchPublisher<SimpleMessage>(_simpleBatchPublisher);
        SystemUnderTest.AddMessageBatchPublisher<AnotherSimpleMessage>(_anotherBatchPublisher);

        // Global middleware (fallback)
        SystemUnderTest.PublishMiddleware = new TrackingMiddleware(() => _globalMiddlewareInvoked = true);

        // Per-type middleware for SimpleMessage only
        SystemUnderTest.AddPublishMiddleware<SimpleMessage>(
            new TrackingMiddleware(() => _perTypeMiddlewareInvoked = true));

        var cts = new CancellationTokenSource(TimeoutPeriod);
        await SystemUnderTest.StartAsync(cts.Token);

        // Batch of SimpleMessage — should use per-type middleware
        await SystemUnderTest.PublishAsync(
            new List<SimpleMessage> { new(), new() },
            new PublishBatchMetadata(),
            CancellationToken.None);

        // Batch of AnotherSimpleMessage — should use global middleware
        await SystemUnderTest.PublishAsync(
            new List<AnotherSimpleMessage> { new(), new() },
            new PublishBatchMetadata(),
            CancellationToken.None);
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
    public void BothBatchPublishersAreCalled()
    {
        _simpleBatchPublisher.Received().PublishAsync(
            Arg.Any<IEnumerable<Message>>(),
            Arg.Any<PublishBatchMetadata>(),
            Arg.Any<CancellationToken>());

        _anotherBatchPublisher.Received().PublishAsync(
            Arg.Any<IEnumerable<Message>>(),
            Arg.Any<PublishBatchMetadata>(),
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
