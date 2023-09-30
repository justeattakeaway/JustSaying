using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging.Middleware.MessageContext;

/// <summary>
/// A middleware that sets context that is available in message handlers by resolving an `IMessageContextAccessor`.
/// </summary>
/// <remarks>
/// Creates an instance of <see cref="MessageContextAccessorMiddleware"/>.
/// </remarks>
/// <param name="messageContextAccessor">The <see cref="IMessageContextAccessor"/> to set.</param>
public sealed class MessageContextAccessorMiddleware(IMessageContextAccessor messageContextAccessor) : MiddlewareBase<HandleMessageContext, bool>
{
    private readonly IMessageContextAccessor _messageContextAccessor = messageContextAccessor ?? throw new ArgumentNullException(nameof(messageContextAccessor));

    protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
    {
        _messageContextAccessor.MessageContext = new MessageHandling.MessageContext(context.RawMessage, context.QueueUri, context.MessageAttributes);

        try
        {
            return await func(stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            _messageContextAccessor.MessageContext = null;
        }
    }
}
