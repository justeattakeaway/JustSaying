using System;
using System.Threading;
using System.Threading.Tasks;

namespace JustSaying.Messaging.Middleware
{
    public abstract class MiddlewareBase<TContext, TOut>
    {
        private readonly MiddlewareBase<TContext, TOut> _next;

        public MiddlewareBase(MiddlewareBase<TContext, TOut> next = null)
        {
            _next = next;
        }

        public async Task<TOut> RunAsync(
            TContext context,
            Func<CancellationToken, Task<TOut>> func,
            CancellationToken stoppingToken)
        {
            return await RunInnerAsync(context,
                async ct =>
                {
                    if (_next == null)
                    {
                        return await func(ct).ConfigureAwait(false);
                    }
                    else
                    {
                        return await _next.RunAsync(context, func, ct).ConfigureAwait(false);
                    }
                },
                stoppingToken).ConfigureAwait(false);
        }

        protected abstract Task<TOut> RunInnerAsync(
            TContext context,
            Func<CancellationToken, Task<TOut>> func,
            CancellationToken stoppingToken);
    }
}
