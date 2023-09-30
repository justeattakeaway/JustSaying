using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Context;

namespace JustSaying.UnitTests.Messaging.Channels.TestHelpers;

internal class FakeDispatcher(Action spy = null) : IMessageDispatcher
{
    public Task DispatchMessageAsync(IQueueMessageContext messageContext, CancellationToken cancellationToken)
    {
        spy?.Invoke();
        DispatchedMessages.Add(messageContext);
        return Task.CompletedTask;
    }

    public IList<IQueueMessageContext> DispatchedMessages { get; } = new List<IQueueMessageContext>();
}
