using System;
using System.Threading.Tasks;
using JustSaying.Messaging.Middleware;
using Polly;

namespace JustSaying.UnitTests.Messaging.Policies.ExamplePolicies
{
    public class PollyMiddleware<TContext, TOut> : MiddlewareBase<TContext, TOut>
    {
        private readonly IAsyncPolicy _policy;

        public PollyMiddleware(MiddlewareBase<TContext, TOut> next, IAsyncPolicy policy) : base(next)
        {
            _policy = policy;
        }

        protected override async Task<TOut> RunInnerAsync(TContext context, Func<Task<TOut>> func)
        {
            return await _policy.ExecuteAsync(func);
        }
    }
}
