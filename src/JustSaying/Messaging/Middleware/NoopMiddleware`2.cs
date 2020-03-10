using System;
using System.Threading.Tasks;

namespace JustSaying.Messaging.Middleware
{
    internal class NoopMiddleware<TContext, TOut> : MiddlewareBase<TContext, TOut>
    {
        public NoopMiddleware() : base(null)
        {
        }

        protected override async Task<TOut> RunInnerAsync(TContext context, Func<Task<TOut>> func)
        {
            return await func().ConfigureAwait(false);
        }
    }
}
