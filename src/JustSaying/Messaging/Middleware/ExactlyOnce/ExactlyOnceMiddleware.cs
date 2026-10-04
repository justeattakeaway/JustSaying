using JustSaying.Extensions;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware;

public sealed class ExactlyOnceMiddleware<T>(IMessageLockAsync messageLock, TimeSpan timeout, string handlerName, Func<T, string> deduplicationKeySelector, ILogger logger) : MiddlewareBase<HandleMessageContext, bool>
{
    // The readable name, not FullName: a generic type's FullName embeds its type arguments' assembly
    // versions, which would change the lock key (and so break deduplication) on every deploy. A
    // non-generic type's readable name is its FullName, so its keys are unchanged.
    private readonly string _lockSuffixKeyForHandler = $"{typeof(T).ToReadableFullName().ToLowerInvariant()}-{handlerName}";
    private readonly Func<T, string> _deduplicationKeySelector = deduplicationKeySelector ?? throw new ArgumentNullException(nameof(deduplicationKeySelector));

    protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (func == null) throw new ArgumentNullException(nameof(func));

        if (context.Message is not T message)
        {
            logger.LogError(
                "Exactly-once handling for message type '{ExpectedMessageType}' received message with Id '{MessageId}' of type '{MessageType}'; returning message to queue.",
                typeof(T).FullName,
                context.RawMessage?.MessageId,
                context.Message?.GetType().FullName);
            return false;
        }

        string deduplicationKey = _deduplicationKeySelector(message);

        if (string.IsNullOrWhiteSpace(deduplicationKey))
        {
            // An empty key would make unrelated messages share one lock and be silently deduplicated,
            // so decline the message and leave it for redrive rather than handling it without a lock.
            logger.LogError(
                "The deduplication key selector for message type '{MessageType}' returned a null or empty key for message with Id '{MessageId}'; returning message to queue.",
                typeof(T).ToReadableFullName(),
                context.RawMessage?.MessageId);
            return false;
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
