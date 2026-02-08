using JustSaying.Messaging;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenBatchPublishingWithMiddleware(ITestOutputHelper outputHelper) : GivenAServiceBus(outputHelper)
{
    private readonly IMessageBatchPublisher _batchPublisher = Substitute.For<IMessageBatchPublisher, IMessagePublisher>();
    private PublishContext _capturedContext;

    protected override async Task WhenAsync()
    {
        SystemUnderTest.AddMessageBatchPublisher<SimpleMessage>(_batchPublisher);
        SystemUnderTest.PublishMiddleware = new CapturingPublishMiddleware(ctx => _capturedContext = ctx);

        var cts = new CancellationTokenSource(TimeoutPeriod);
        await SystemUnderTest.StartAsync(cts.Token);

        var messages = new List<SimpleMessage> { new(), new(), new() };
        await SystemUnderTest.PublishAsync(messages, new PublishBatchMetadata(), CancellationToken.None);
    }

    [Fact]
    public void MiddlewareReceivesBatchContext()
    {
        _capturedContext.ShouldNotBeNull();
        _capturedContext.Messages.ShouldNotBeNull();
        _capturedContext.Messages.Count.ShouldBe(3);
    }

    [Fact]
    public void BatchPublisherIsCalled()
    {
        _batchPublisher.Received().PublishAsync(
            Arg.Any<IEnumerable<Message>>(),
            Arg.Any<PublishBatchMetadata>(),
            Arg.Any<CancellationToken>());
    }

    private class CapturingPublishMiddleware(Action<PublishContext> onInvoked) : MiddlewareBase<PublishContext, bool>
    {
        protected override async Task<bool> RunInnerAsync(
            PublishContext context,
            Func<CancellationToken, Task<bool>> func,
            CancellationToken stoppingToken)
        {
            onInvoked(context);
            return await func(stoppingToken).ConfigureAwait(false);
        }
    }
}
