using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.UnitTests.Messaging.Channels.TestHelpers
{
    public class FakeMessageLock : IMessageLockAsync
    {
        public FakeMessageLock()
        {
            MessageLockRequests = new List<(string key, TimeSpan howLong, bool isPermanent)>();
        }

        public IList<(string key, TimeSpan howLong, bool isPermanent)> MessageLockRequests { get; }

        public Task<MessageLockResponse> TryAquireLockPermanentlyAsync(string key)
        {
            MessageLockRequests.Add((key, TimeSpan.MaxValue, true));
            return Task.FromResult(new MessageLockResponse
            {
                DoIHaveExclusiveLock = true
            });
        }

        public Task<MessageLockResponse> TryAquireLockAsync(string key, TimeSpan howLong)
        {
            MessageLockRequests.Add((key, howLong, false));
            return Task.FromResult(new MessageLockResponse
            {
                DoIHaveExclusiveLock = true
            });
        }

        public Task ReleaseLockAsync(string key)
        {
            return Task.CompletedTask;
        }
    }
}
