using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.Messaging.Middleware.Handle
{
    public class HandlerInvocationMiddleware<T> : MiddlewareBase<HandleMessageContext, bool>
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

            //TODO: cache these
            Type handlerType = handler.GetType();
            MethodInfo handleMethod = handlerType.GetMethod("Handle");

            if (handleMethod == null)
                throw new InvalidOperationException(
                    $"No Handle method found on handler type {handlerType.Name}");

            var task = (Task<bool>) handleMethod.Invoke(handler, new object[] { context.Message });

            return await task.ConfigureAwait(false);
        }
    }
}
