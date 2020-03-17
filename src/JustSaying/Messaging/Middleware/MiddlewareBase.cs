using System;
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

        public async Task<TOut> RunAsync(TContext context, Func<Task<TOut>> func)
        {
            return await RunInnerAsync(context, async () =>
            {
                if (_next == null)
                {
                    return await func().ConfigureAwait(false);
                }
                else
                {
                    return await _next.RunAsync(context, func).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }

        protected abstract Task<TOut> RunInnerAsync(TContext context, Func<Task<TOut>> func);
    }
}
