using System;
using System.Threading.Tasks;

namespace JustSaying.Messaging.Middleware
{
    internal class DelegateMiddleware<TContext, TOut> : MiddlewareBase<TContext, TOut>
    {
        protected override async Task<TOut> RunInnerAsync(TContext context, Func<Task<TOut>> func)
        {
            return await func().ConfigureAwait(false);
        }
    }
}
