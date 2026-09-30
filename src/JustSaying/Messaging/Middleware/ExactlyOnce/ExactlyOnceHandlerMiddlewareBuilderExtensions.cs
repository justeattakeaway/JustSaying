using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.HandleMessageContext, bool>;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware;

public static class ExactlyOnceHandlerMiddlewareBuilderExtensions
{
    /// <summary>
    /// Adds an <see cref="ExactlyOnceMiddleware{T}"/> to the current pipeline.
    /// </summary>
    /// <param name="builder">The current <see cref="HandlerMiddlewareBuilder"/>.</param>
    /// <param name="lockKey">A unique key to identify this lock, e.g. the name of the handler.</param>
    /// <param name="lockDuration">The length of time to lock messages while handling them.</param>
    /// <param name="deduplicationKeySelector">
    /// An optional selector that returns a stable, per-message key used to deduplicate handling. When
    /// <see langword="null"/>, messages deriving from <see cref="Message"/> use
    /// <see cref="Message.UniqueKey"/>. A selector is <em>required</em> for message types that do not
    /// derive from <see cref="Message"/>, otherwise this method throws.
    /// </param>
    /// <typeparam name="TMessage">The type of the message that should be locked.</typeparam>
    /// <returns>The current <see cref="HandlerMiddlewareBuilder"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="TMessage"/> does not derive from <see cref="Message"/> and no
    /// <paramref name="deduplicationKeySelector"/> was provided, so no stable deduplication key is
    /// available.
    /// </exception>
    public static HandlerMiddlewareBuilder UseExactlyOnce<TMessage>(
        this HandlerMiddlewareBuilder builder,
        string lockKey,
        TimeSpan? lockDuration = null,
        Func<TMessage, string> deduplicationKeySelector = null)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (string.IsNullOrEmpty(lockKey)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(lockKey));

        Func<TMessage, string> keySelector = deduplicationKeySelector ?? CreateDefaultKeySelector<TMessage>(lockKey);

        HandleMessageMiddleware CreateMiddleware()
        {
            var messageLock = builder.ServiceResolver.ResolveService<IMessageLockAsync>();
            var logger = builder.ServiceResolver.ResolveService<ILogger<ExactlyOnceMiddleware<TMessage>>>();

            return new ExactlyOnceMiddleware<TMessage>(messageLock,
                lockDuration ?? TimeSpan.MaxValue,
                lockKey,
                keySelector,
                logger);
        }

        builder.Use(CreateMiddleware);

        return builder;
    }

    private static Func<TMessage, string> CreateDefaultKeySelector<TMessage>(string lockKey)
    {
        if (!typeof(Message).IsAssignableFrom(typeof(TMessage)))
        {
            throw new InvalidOperationException(
                $"Exactly-once handling requires a stable deduplication key for message type '{typeof(TMessage).FullName}', " +
                $"which does not derive from {typeof(Message).FullName}. Provide one via " +
                $"UseExactlyOnce<{typeof(TMessage).Name}>(\"{lockKey}\", deduplicationKeySelector: m => ...).");
        }

        return static message => ((Message)(object)message).UniqueKey();
    }
}