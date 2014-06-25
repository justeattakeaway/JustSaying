using System;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageHandling
{
    public class ExactlyOnceHandler<T> : IHandler<T> where T : Message
    {
        private readonly IHandler<T> _inner;
        private readonly IMessageLock _messageLock;
        private const int TEMPORARY_LOCK_SECONDS = 15;

        public ExactlyOnceHandler(IHandler<T>  inner, IMessageLock messageLock)
        {
            _inner = inner;
            _messageLock = messageLock;
        }

        public bool Handle(T message)
        {
            var lockKey = message.Id.ToString();
            bool canLock = _messageLock.TryAquire(lockKey, TimeSpan.FromSeconds(TEMPORARY_LOCK_SECONDS));
            if (!canLock)
                return true;

            try
            {
                var successfullyHandled = _inner.Handle(message);
                if (successfullyHandled)
                {
                    _messageLock.TryAquire(lockKey);
                }
                return successfullyHandled;
            }
            catch
            {
                _messageLock.Release(lockKey);
                throw;
            }
        }
    }
}
