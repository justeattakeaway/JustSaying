using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware;

/// <summary>
/// This middleware is responsible for recalling a previously resolved message handler and calling it.
/// This class is obsolete and will be removed in a future version. Please use HandlerResolveInvocationMiddleware instead.
/// </summary>
/// <typeparam name="T">The type of the message that the message handler handles.</typeparam>
public sealed class HandlerInvocationMiddleware<T>(Func<HandlerResolutionContext, IHandlerAsync<T>> handlerResolver) : MiddlewareBase<HandleMessageContext, bool> where T : Message
{
    private readonly Func<HandlerResolutionContext, IHandlerAsync<T>> _handlerResolver = handlerResolver ?? throw new ArgumentNullException(nameof(handlerResolver));

    protected override async Task<bool> RunInnerAsync(
        HandleMessageContext context,
        Func<CancellationToken, Task<bool>> func,
        CancellationToken stoppingToken)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        stoppingToken.ThrowIfCancellationRequested();

        var resolutionContext = new HandlerResolutionContext(context.QueueName);

        IHandlerAsync<T> handler = _handlerResolver(resolutionContext);

        return await handler.Handle(context.MessageAs<T>()).ConfigureAwait(false);
    }
}
