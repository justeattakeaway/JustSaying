using System;
using System.Threading.Tasks;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageHandling
{
    public class ExactlyOnceHandler<T> : IHandlerAsync<T> where T : Message
    {
        private readonly IHandlerAsync<T> _inner;
        private readonly IMessageLockAsync _messageLock;
        private readonly TimeSpan _timeout;
        private readonly string _lockSuffixKeyForHandler;

        public ExactlyOnceHandler(IHandlerAsync<T> inner, IMessageLockAsync messageLock, TimeSpan timeout, string handlerName)
        {
            _inner = inner;
            _messageLock = messageLock;
            _timeout = timeout;
            _lockSuffixKeyForHandler = $"{typeof(T).ToString().ToLowerInvariant()}-{handlerName}";
        }

        private const bool RemoveTheMessageFromTheQueue = true;
        private const bool LeaveItInTheQueue = false;

        public async Task<bool> Handle(T message)
        {
            string lockKey = $"{message.UniqueKey()}-{_lockSuffixKeyForHandler}";
            MessageLockResponse lockResponse = await _messageLock.TryAquireLockAsync(lockKey, _timeout).ConfigureAwait(false);

            if (!lockResponse.DoIHaveExclusiveLock)
            {
                if (lockResponse.IsMessagePermanentlyLocked)
                {
                    return RemoveTheMessageFromTheQueue;
                }

                return LeaveItInTheQueue;
            }

            try
            {
                bool successfullyHandled = await _inner.Handle(message).ConfigureAwait(false);

                if (successfullyHandled)
                {
                    await _messageLock.TryAquireLockPermanentlyAsync(lockKey).ConfigureAwait(false);
                }

                return successfullyHandled;
            }
            catch (Exception)
            {
                await _messageLock.ReleaseLockAsync(lockKey).ConfigureAwait(false);
                throw;
            }
        }
    }
}
