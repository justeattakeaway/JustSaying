using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware;

public sealed class ExactlyOnceMiddleware<T>(IMessageLockAsync messageLock, TimeSpan timeout, string handlerName, ILogger logger) : MiddlewareBase<HandleMessageContext, bool>
{
    private readonly string _lockSuffixKeyForHandler = $"{typeof(T).FullName.ToLowerInvariant()}-{handlerName}";

    protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (func == null) throw new ArgumentNullException(nameof(func));

        string lockKey = $"{context.Message.UniqueKey()}-{_lockSuffixKeyForHandler}";

        MessageLockResponse lockResponse = await messageLock.TryAcquireLockAsync(lockKey, timeout).ConfigureAwait(false);

        if (!lockResponse.DoIHaveExclusiveLock)
        {
            if (lockResponse.IsMessagePermanentlyLocked)
            {
                logger.LogDebug("Failed to acquire lock for message with key {MessageLockKey} as it is permanently locked.", lockKey);
                return true;
            }

            logger.LogDebug("Failed to acquire lock for message with key {MessageLockKey}; returning message to queue.", lockKey);
            return false;
        }

        try
        {
            logger.LogDebug("Acquired lock for message with key {MessageLockKey}.", lockKey);

            bool successfullyHandled = await func(stoppingToken).ConfigureAwait(false);

            if (successfullyHandled)
            {
                await messageLock.TryAcquireLockPermanentlyAsync(lockKey).ConfigureAwait(false);

                logger.LogDebug("Acquired permanent lock for message with key {MessageLockKey}.", lockKey);
            }

            return successfullyHandled;
        }
        catch (Exception)
        {
            await messageLock.ReleaseLockAsync(lockKey).ConfigureAwait(false);
            logger.LogDebug("Released lock for message with key {MessageLockKey}.", lockKey);
            throw;
        }
    }
}
