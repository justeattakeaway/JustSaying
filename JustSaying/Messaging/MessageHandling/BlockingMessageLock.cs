using System;
using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageHandling
{
    /// <summary>
    /// Used to convert "IMessageLock " instances into IMessageLockAsync
    /// So that the rest of the system only has to deal with IMessageLockAsync
    /// </summary>
    public class BlockingMessageLock : IMessageLockAsync
    {
        public BlockingMessageLock(IMessageLock inner)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            Inner = inner;
        }

        public IMessageLock Inner { get; }

        public Task ReleaseLockAsync(string key)
        {
            Inner.ReleaseLock(key);
            return Task.FromResult(0);
        }

        public Task<MessageLockResponse> TryAquireLockAsync(string key, TimeSpan howLong) => Task.FromResult(Inner.TryAquireLock(key, howLong));

        public Task<MessageLockResponse> TryAquireLockPermanentlyAsync(string key) => Task.FromResult(Inner.TryAquireLockPermanently(key));
    }
}
