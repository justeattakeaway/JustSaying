using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware
{
    public class ExactlyOnceMiddleware<T> : MiddlewareBase<HandleMessageContext, bool>
    {
        private readonly IMessageLockAsync _messageLock;
        private readonly TimeSpan _timeout;
        private readonly string _lockSuffixKeyForHandler;
        private readonly ILogger _logger;

        private const bool RemoveTheMessageFromTheQueue = true;
        private const bool LeaveItInTheQueue = false;

        public ExactlyOnceMiddleware(IMessageLockAsync messageLock, TimeSpan timeout, string handlerName, ILogger logger)
        {
            _messageLock = messageLock;
            _timeout = timeout;
            _logger = logger;

            _lockSuffixKeyForHandler = $"{typeof(T).FullName.ToLowerInvariant()}-{handlerName}";
        }

        protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (func == null) throw new ArgumentNullException(nameof(func));

            string lockKey = $"{context.Message.UniqueKey()}-{_lockSuffixKeyForHandler}";

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

                bool successfullyHandled = await func(stoppingToken).ConfigureAwait(false);

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
