using System;
using System.Collections.Generic;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    internal class MessageLockStore : IMessageLock
    {
        private readonly Dictionary<string, int> _store = new Dictionary<string, int>();

        public MessageLockResponse TryAquireLockPermanently(string key)
        {
            var canAquire = !_store.TryGetValue(key, out int value);

            if (canAquire)
            {
                _store.Add(key, 1);
            }

            return new MessageLockResponse { DoIHaveExclusiveLock = canAquire };
        }

        public MessageLockResponse TryAquireLock(string key, TimeSpan howLong)
            => TryAquireLockPermanently(key);

        public void ReleaseLock(string key)
            => _store.Remove(key);
    }
}
