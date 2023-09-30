using JustSaying.Messaging.Middleware;
using JustSaying.Models;

namespace JustSaying.UnitTests.AwsTools.MessageHandling;

/// <summary>
/// A utility middleware that removes the need for handler boilerplate, and creates an inline handler pipeline
/// </summary>
/// <typeparam name="T"></typeparam>
public class DelegateMessageHandlingMiddleware<TMessage>(Func<TMessage, Task<bool>> func) : MiddlewareBase<HandleMessageContext, bool> where TMessage : Message
{
    private readonly Func<TMessage, Task<bool>> _func = func;

    protected override async Task<bool> RunInnerAsync(
        HandleMessageContext context,
        Func<CancellationToken, Task<bool>> func,
        CancellationToken stoppingToken)
    {
        if(context == null) throw new ArgumentNullException(nameof(context));

        return await _func(context.MessageAs<TMessage>());
    }
}