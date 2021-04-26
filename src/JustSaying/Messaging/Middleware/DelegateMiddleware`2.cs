using System;
using System.Threading;
using System.Threading.Tasks;

namespace JustSaying.Messaging.Middleware
{
    internal class DelegateMiddleware<TContext, TOut> : MiddlewareBase<TContext, TOut>
    {
        protected override Task<TOut> RunInnerAsync(TContext context, Func<CancellationToken, Task<TOut>> func, CancellationToken stoppingToken)
        {
            return func(stoppingToken);
        }
    }
}
