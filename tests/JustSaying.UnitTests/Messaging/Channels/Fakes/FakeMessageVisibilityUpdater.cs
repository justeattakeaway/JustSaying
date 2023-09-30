using JustSaying.Messaging.Channels.Context;

namespace JustSaying.UnitTests.Messaging.Channels.Fakes;

public class FakeVisibilityUpdater : IMessageVisibilityUpdater
{
    public Task UpdateMessageVisibilityTimeout(TimeSpan visibilityTimeout, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
