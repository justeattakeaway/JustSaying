using System.Collections.Concurrent;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.UnitTests.Messaging.Channels.TestHelpers;

public class FakeMessageLock(bool exclusive = true) : IMessageLockAsync
{
    private readonly ConcurrentBag<(string key, TimeSpan howLong, bool isPermanent)> _messageLockRequests = [];

    public IReadOnlyCollection<(string key, TimeSpan howLong, bool isPermanent)> MessageLockRequests => _messageLockRequests;

    public Task<MessageLockResponse> TryAcquireLockPermanentlyAsync(string key)
    {
        _messageLockRequests.Add((key, TimeSpan.MaxValue, true));
        return Task.FromResult(new MessageLockResponse
        {
            DoIHaveExclusiveLock = exclusive
        });
    }

    public Task<MessageLockResponse> TryAcquireLockAsync(string key, TimeSpan howLong)
    {
        _messageLockRequests.Add((key, howLong, false));
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
