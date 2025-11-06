using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.UnitTests.Messaging.Channels.TestHelpers
{
    public class FakeMessageLock : IMessageLockAsync
    {
        private readonly bool _exclusive;

        public FakeMessageLock(bool exclusive = true)
        {
            _exclusive = exclusive;
            MessageLockRequests = new List<(string key, TimeSpan howLong, bool isPermanent)>();
        }

        public IList<(string key, TimeSpan howLong, bool isPermanent)> MessageLockRequests { get; }

        public Task<MessageLockResponse> TryAcquireLockPermanentlyAsync(string key)
        {
            MessageLockRequests.Add((key, TimeSpan.MaxValue, true));
            return Task.FromResult(new MessageLockResponse
            {
                DoIHaveExclusiveLock = _exclusive
            });
        }

        public Task<MessageLockResponse> TryAcquireLockAsync(string key, TimeSpan howLong)
        {
            MessageLockRequests.Add((key, howLong, false));
            return Task.FromResult(new MessageLockResponse
            {
                DoIHaveExclusiveLock = _exclusive
            });
        }

        public Task ReleaseLockAsync(string key)
        {
            return Task.CompletedTask;
        }
    }
}
