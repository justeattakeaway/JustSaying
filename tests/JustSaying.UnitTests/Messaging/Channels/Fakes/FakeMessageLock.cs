using JustSaying.Messaging.MessageHandling;

namespace JustSaying.UnitTests.Messaging.Channels.TestHelpers;

public class FakeMessageLock(bool exclusive = true) : IMessageLockAsync
{
    public IList<(string key, TimeSpan howLong, bool isPermanent)> MessageLockRequests { get; } = new List<(string key, TimeSpan howLong, bool isPermanent)>();

    public Task<MessageLockResponse> TryAcquireLockPermanentlyAsync(string key)
    {
        MessageLockRequests.Add((key, TimeSpan.MaxValue, true));
        return Task.FromResult(new MessageLockResponse
        {
            DoIHaveExclusiveLock = exclusive
        });
    }

    public Task<MessageLockResponse> TryAcquireLockAsync(string key, TimeSpan howLong)
    {
        MessageLockRequests.Add((key, howLong, false));
        return Task.FromResult(new MessageLockResponse
        {
            DoIHaveExclusiveLock = exclusive
        });
    }

    public Task ReleaseLockAsync(string key)
    {
        return Task.CompletedTask;
    }
}
