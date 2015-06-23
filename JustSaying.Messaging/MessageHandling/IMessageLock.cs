using System;

namespace JustSaying.Messaging.MessageHandling
{
    public class MessageLockResponse
    {
        public bool DoIHaveExclusiveLock { get; set; }
        public bool IsMessagePermanentlyLocked{ get; set; }
        public DateTimeOffset ExpiryAt { get; set; }
        public long ExpiryAtTicks { get; set; }

        public override string ToString()
        {
            if (DoIHaveExclusiveLock)
            {
                return string.Format("Message is exclusively locked. The expiry is {0}, {1} ticks.", ExpiryAt, ExpiryAtTicks);
            }

            return string.Format("Message could NOT be exclusively locked. The lock will expire at {0}, {1} ticks.", ExpiryAt, ExpiryAtTicks);
        }
    }

    public interface IMessageLock
    {
        MessageLockResponse TryAquireLockPermanently(string key);
        MessageLockResponse TryAquireLock(string key, TimeSpan howLong);
        void ReleaseLock(string key);
    }
}