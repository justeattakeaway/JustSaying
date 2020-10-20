using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.Messaging.Middleware.Handle
{
    public class HandlerInvocationMiddleware<T> : MiddlewareBase<HandleMessageContext, bool> where T : Message
    {
        private readonly Func<HandlerResolutionContext, IHandlerAsync<T>> _handlerResolver;

        public HandlerInvocationMiddleware(Func<HandlerResolutionContext, IHandlerAsync<T>> handlerResolver)
        {
            _handlerResolver = handlerResolver;
        }

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
}
