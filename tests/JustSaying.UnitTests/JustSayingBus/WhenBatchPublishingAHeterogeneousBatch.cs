using JustSaying.Messaging;
using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

/// <summary>
/// A single batch may contain more than one message type. Each runtime-type group must be routed
/// through the middleware registered for <em>that</em> type, not through whichever pipeline the first
/// message in the batch happened to select.
/// </summary>
public class WhenBatchPublishingAHeterogeneousBatch : GivenAServiceBus
{
    private readonly IMessageBatchPublisher _simpleBatchPublisher = Substitute.For<IMessageBatchPublisher, IMessagePublisher>();
    private readonly IMessageBatchPublisher _anotherBatchPublisher = Substitute.For<IMessageBatchPublisher, IMessagePublisher>();
    private readonly List<object> _simpleMiddlewareSawMessages = [];
    private readonly List<object> _anotherMiddlewareSawMessages = [];

    protected override async Task WhenAsync()
    {
        SystemUnderTest.AddMessageBatchPublisher<SimpleMessage>(_simpleBatchPublisher);
        SystemUnderTest.AddMessageBatchPublisher<AnotherSimpleMessage>(_anotherBatchPublisher);

        SystemUnderTest.AddPublishMiddleware<SimpleMessage>(
            new RecordingMiddleware(_simpleMiddlewareSawMessages));
        SystemUnderTest.AddPublishMiddleware<AnotherSimpleMessage>(
            new RecordingMiddleware(_anotherMiddlewareSawMessages));

        var cts = new CancellationTokenSource(TimeoutPeriod);
        await SystemUnderTest.StartAsync(cts.Token);

        await SystemUnderTest.PublishBatchAsync<Message>(
            [new SimpleMessage(), new AnotherSimpleMessage(), new SimpleMessage()],
            new PublishBatchMetadata(),
            CancellationToken.None);
    }

    [Test]
    public void EachTypesMiddlewareOnlySeesItsOwnMessages()
    {
        _simpleMiddlewareSawMessages.Count.ShouldBe(2);
        _simpleMiddlewareSawMessages.ShouldAllBe(x => x is SimpleMessage);

        _anotherMiddlewareSawMessages.Count.ShouldBe(1);
        _anotherMiddlewareSawMessages.ShouldAllBe(x => x is AnotherSimpleMessage);
    }

    [Test]
    public void EachTypesPublisherIsCalled()
    {
        _simpleBatchPublisher.Received(1).PublishBatchAsync(
            Arg.Any<IEnumerable<Message>>(),
            Arg.Any<PublishBatchMetadata>(),
            Arg.Any<CancellationToken>());

        _anotherBatchPublisher.Received(1).PublishBatchAsync(
            Arg.Any<IEnumerable<Message>>(),
            Arg.Any<PublishBatchMetadata>(),
            Arg.Any<CancellationToken>());
    }

    private class RecordingMiddleware(List<object> seen) : MiddlewareBase<PublishContext, bool>
    {
        protected override async Task<bool> RunInnerAsync(
            PublishContext context,
            Func<CancellationToken, Task<bool>> func,
            CancellationToken stoppingToken)
        {
            lock (seen)
            {
                seen.AddRange(context.Messages);
            }

            return await func(stoppingToken).ConfigureAwait(false);
        }
    }
}
