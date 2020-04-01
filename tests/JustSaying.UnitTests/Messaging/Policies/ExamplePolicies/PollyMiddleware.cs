using System;
using System.Threading;
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

        protected override async Task<TOut> RunInnerAsync(
            TContext context,
            Func<CancellationToken, Task<TOut>> func,
            CancellationToken stoppingToken)
        {
            return await _policy.ExecuteAsync(() => func(stoppingToken));
        }
    }
}
