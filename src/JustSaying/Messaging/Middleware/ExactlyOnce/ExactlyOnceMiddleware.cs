using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware;

public sealed class ExactlyOnceMiddleware<T>(IMessageLockAsync messageLock, TimeSpan timeout, string handlerName, Func<T, string> deduplicationKeySelector, ILogger logger) : MiddlewareBase<HandleMessageContext, bool>
{
    private readonly string _lockSuffixKeyForHandler = $"{typeof(T).FullName.ToLowerInvariant()}-{handlerName}";
    private readonly Func<T, string> _deduplicationKeySelector = deduplicationKeySelector ?? throw new ArgumentNullException(nameof(deduplicationKeySelector));

    protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (func == null) throw new ArgumentNullException(nameof(func));

        string deduplicationKey = _deduplicationKeySelector((T)context.Message);

        if (string.IsNullOrWhiteSpace(deduplicationKey))
        {
            throw new InvalidOperationException(
                $"The deduplication key selector for message type '{typeof(T).FullName}' returned a null or empty key. " +
                "Exactly-once handling requires a stable, non-empty key per message, otherwise unrelated messages " +
                "would share a lock and be silently deduplicated.");
        }

        string lockKey = $"{deduplicationKey}-{_lockSuffixKeyForHandler}";

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
