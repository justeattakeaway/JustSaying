using System.Collections.Concurrent;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Context;

namespace JustSaying.UnitTests.Messaging.Channels.TestHelpers;

internal class FakeDispatcher(Action spy = null) : IMessageDispatcher
{
    private readonly ConcurrentBag<IQueueMessageContext> _dispatchedMessages = [];

    public Task DispatchMessageAsync(IQueueMessageContext messageContext, CancellationToken cancellationToken)
    {
        spy?.Invoke();
        _dispatchedMessages.Add(messageContext);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<IQueueMessageContext> DispatchedMessages => _dispatchedMessages;
}
