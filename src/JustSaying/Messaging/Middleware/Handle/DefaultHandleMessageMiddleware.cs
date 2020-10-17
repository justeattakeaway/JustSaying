using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.Messaging.Middleware.Handle
{
    public abstract class HandleMessageMiddleware : MiddlewareBase<HandleMessageContext, bool>
    { }

    public class DefaultHandleMessageMiddleware<T> : HandleMessageMiddleware
    {
        private Func<IHandlerAsync<T>> _handlerResolver;

        public DefaultHandleMessageMiddleware(Func<IHandlerAsync<T>> handlerResolver)
        {
            _handlerResolver = handlerResolver;
        }

        protected override async Task<bool> RunInnerAsync(
            HandleMessageContext context,
            Func<CancellationToken, Task<bool>> func,
            CancellationToken stoppingToken)
        {
            var handler = _handlerResolver();

            var handlerType = handler.GetType().getgen
            
            var message = context.Message< >()
            await handler.Handle()

            return await func(stoppingToken).ConfigureAwait(false);
        }
    }
}
