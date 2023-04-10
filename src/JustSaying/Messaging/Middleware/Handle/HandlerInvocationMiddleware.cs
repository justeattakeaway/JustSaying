using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware;

/// <summary>
/// This middleware is responsible for resolving a message handler and calling it.
/// </summary>
/// <typeparam name="TMessage">The type of the message that the message handler handles.</typeparam>
public sealed class HandlerInvocationMiddleware<TMessage> : MiddlewareBase<HandleMessageContext, bool> where TMessage : class
{
    private readonly Func<HandlerResolutionContext, IHandlerAsync<TMessage>> _handlerResolver;

    public HandlerInvocationMiddleware(Func<HandlerResolutionContext, IHandlerAsync<TMessage>> handlerResolver)
    {
        _handlerResolver = handlerResolver ?? throw new ArgumentNullException(nameof(handlerResolver));
    }

    protected override async Task<bool> RunInnerAsync(
        HandleMessageContext context,
        Func<CancellationToken, Task<bool>> func,
        CancellationToken stoppingToken)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        stoppingToken.ThrowIfCancellationRequested();

        var resolutionContext = new HandlerResolutionContext(context.QueueName);

        IHandlerAsync<TMessage> handler = _handlerResolver(resolutionContext);

        return await handler.Handle(context.MessageAs<TMessage>()).ConfigureAwait(false);
    }
}
