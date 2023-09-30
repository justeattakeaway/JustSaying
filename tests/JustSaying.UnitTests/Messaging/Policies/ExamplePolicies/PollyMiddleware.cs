using JustSaying.Messaging.Middleware;
using Polly;

namespace JustSaying.UnitTests.Messaging.Policies.ExamplePolicies;

public class PollyMiddleware<TContext, TOut>(ResiliencePipeline pipeline) : MiddlewareBase<TContext, TOut>
{
    protected override async Task<TOut> RunInnerAsync(
        TContext context,
        Func<CancellationToken, Task<TOut>> func,
        CancellationToken stoppingToken)
    {
        var pool = ResilienceContextPool.Shared;
        var resilienceContext = pool.Get(stoppingToken);

        try
        {
            return await pipeline.ExecuteAsync(
                static async (context, func) =>
                    await func(context.CancellationToken), resilienceContext, func);
        }
        finally
        {
            pool.Return(resilienceContext);
        }
    }
}
