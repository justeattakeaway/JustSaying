using JustSaying.Messaging.Middleware;
using Polly;

namespace JustSaying.UnitTests.Messaging.Policies.ExamplePolicies;

public class PollyMiddleware<TContext, TOut>(IAsyncPolicy policy) : MiddlewareBase<TContext, TOut>
{
    protected override async Task<TOut> RunInnerAsync(
        TContext context,
        Func<CancellationToken, Task<TOut>> func,
        CancellationToken stoppingToken)
    {
        return await policy.ExecuteAsync(() => func(stoppingToken));
    }
}
