using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public sealed class MessageLockStore : IMessageLockAsync
    {
        private readonly ConcurrentDictionary<string, int> _store = new ConcurrentDictionary<string, int>();

        public Task<MessageLockResponse> TryAquireLockAsync(string key, TimeSpan howLong)
        {
            // Only the first attempt to access the value for the key can acquire the lock
            int newValue = _store.AddOrUpdate(key, 0, (_, i) => i + 1);

            var response = new MessageLockResponse
            {
                DoIHaveExclusiveLock = newValue == 0,
                IsMessagePermanentlyLocked = newValue == int.MinValue,
            };

            return Task.FromResult(response);
        }

        public Task<MessageLockResponse> TryAquireLockPermanentlyAsync(string key)
        {
            _store.AddOrUpdate(key, int.MinValue, (_, i) => int.MinValue);

            var response = new MessageLockResponse
            {
                DoIHaveExclusiveLock = true,
                IsMessagePermanentlyLocked = true,
            };

            return Task.FromResult(response);
        }

        public Task ReleaseLockAsync(string key)
        {
            _ = _store.Remove(key, out _);
            return Task.CompletedTask;
        }
    }
}
