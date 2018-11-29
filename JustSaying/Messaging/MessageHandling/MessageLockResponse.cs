using System;

namespace JustSaying.Messaging.MessageHandling
{
    public class MessageLockResponse
    {
        public bool DoIHaveExclusiveLock { get; set; }

        public bool IsMessagePermanentlyLocked { get; set; }

        public DateTimeOffset ExpiryAt { get; set; }

        public long ExpiryAtTicks { get; set; }

        public override string ToString()
            => DoIHaveExclusiveLock
            ? $"Message is exclusively locked. The expiry is {ExpiryAt}, {ExpiryAtTicks} ticks."
            : $"Message could NOT be exclusively locked. The lock will expire at {ExpiryAt}, {ExpiryAtTicks} ticks.";
    }
}
