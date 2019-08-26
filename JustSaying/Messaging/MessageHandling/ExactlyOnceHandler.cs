using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageHandling
{
    public class ExactlyOnceHandler<T> : IHandlerAsync<T>, ICancellableHandlerAsync<T>
        where T : Message
    {
        private static readonly string MessageTypeKey = typeof(T).ToString().ToLowerInvariant();

        private readonly Func<T, CancellationToken, Task<bool>> _inner;
        private readonly IMessageLockAsync _messageLock;
        private readonly TimeSpan _timeout;
        private readonly string _lockSuffixKeyForHandler;

        public ExactlyOnceHandler(IHandlerAsync<T> inner, IMessageLockAsync messageLock, TimeSpan timeout, string handlerName)
        {
            _messageLock = messageLock;
            _timeout = timeout;
            _lockSuffixKeyForHandler = $"{MessageTypeKey}-{handlerName}";

            if (inner is ICancellableHandlerAsync<T> cancellable)
            {
                _inner = cancellable.HandleAsync;
            }
            else
            {
                _inner = async (message, _) => await inner.Handle(message).ConfigureAwait(false);
            }
        }

        private const bool RemoveTheMessageFromTheQueue = true;
        private const bool LeaveItInTheQueue = false;

        public async Task<bool> Handle(T message)
        {
            return await HandleAsync(message, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<bool> HandleAsync(T message, CancellationToken cancellationToken)
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
                bool successfullyHandled = await _inner(message, cancellationToken).ConfigureAwait(false);

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
