using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    internal class MessageLockStore : IMessageLockAsync
    {
        private readonly Dictionary<string, int> _store = new Dictionary<string, int>();

        public Task<MessageLockResponse> TryAquireLockPermanentlyAsync(string key)
        {
            var canAquire = !_store.TryGetValue(key, out int value);

            if (canAquire)
            {
                _store.Add(key, 1);
            }

            var response = new MessageLockResponse { DoIHaveExclusiveLock = canAquire };

            return Task.FromResult(response);
        }

        public Task<MessageLockResponse> TryAquireLockAsync(string key, TimeSpan howLong)
            => TryAquireLockPermanentlyAsync(key);

        public Task ReleaseLockAsync(string key)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }
    }
}
