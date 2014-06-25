using System;

namespace JustSaying.Messaging.MessageHandling
{
    public interface IMessageLock
    {
        bool TryAquire(string key);
        bool TryAquire(string key, TimeSpan howLong);
        void Release(string key);
    }
}