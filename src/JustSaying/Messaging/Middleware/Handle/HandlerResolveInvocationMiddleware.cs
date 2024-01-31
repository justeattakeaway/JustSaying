using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware;

/// <summary>
/// This middleware is responsible for resolving a message handler and calling it.
/// </summary>
/// <typeparam name="T">The type of the message that the message handler handles.</typeparam>
public sealed class HandlerResolveInvocationMiddleware<T>(IHandlerResolver handlerResolver) : MiddlewareBase<HandleMessageContext, bool> where T : Message
{
    /// <inheritdoc />
    protected override async Task<bool> RunInnerAsync(
        HandleMessageContext context,
        Func<CancellationToken, Task<bool>> func,
        CancellationToken stoppingToken)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        stoppingToken.ThrowIfCancellationRequested();

        var resolutionContext = new HandlerResolutionContext(context.QueueName);

        var handler = handlerResolver.ResolveHandler<T>(resolutionContext);

        return await handler.Handle(context.MessageAs<T>()).ConfigureAwait(false);
    }
}
