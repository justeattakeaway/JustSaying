using System;
using System.Threading.Tasks;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.MessageHandling
{
    internal sealed class ExactlyOnceHandler<T> : IHandlerAsync<T> where T : Message
    {
        private readonly IHandlerAsync<T> _inner;
        private readonly IMessageLockAsync _messageLock;
        private readonly TimeSpan _timeout;
        private readonly string _lockSuffixKeyForHandler;
        private readonly ILogger _logger;

        public ExactlyOnceHandler(
            IHandlerAsync<T> inner,
            IMessageLockAsync messageLock,
            TimeSpan timeout,
            string handlerName,
            ILogger<ExactlyOnceHandler<T>> logger)
        {
            _inner = inner;
            _messageLock = messageLock;
            _timeout = timeout;
            _lockSuffixKeyForHandler = $"{typeof(T).ToString().ToLowerInvariant()}-{handlerName}";
            _logger = logger;
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
                    _logger.LogDebug("Failed to acquire lock for message with key {MessageLockKey} as it is permanently locked.", lockKey);
                    return RemoveTheMessageFromTheQueue;
                }

                _logger.LogDebug("Failed to acquire lock for message with key {MessageLockKey}; returning message to queue.", lockKey);
                return LeaveItInTheQueue;
            }

            try
            {
                _logger.LogDebug("Acquired lock for message with key {MessageLockKey}.", lockKey);

                bool successfullyHandled = await _inner.Handle(message).ConfigureAwait(false);

                if (successfullyHandled)
                {
                    await _messageLock.TryAquireLockPermanentlyAsync(lockKey).ConfigureAwait(false);

                    _logger.LogDebug("Acquired permanent lock for message with key {MessageLockKey}.", lockKey);
                }

                return successfullyHandled;
            }
            catch (Exception)
            {
                await _messageLock.ReleaseLockAsync(lockKey).ConfigureAwait(false);
                _logger.LogDebug("Released lock for message with key {MessageLockKey}.", lockKey);
                throw;
            }
        }
    }
}
