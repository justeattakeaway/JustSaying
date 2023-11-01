using JustSaying.Messaging.Middleware;
using Polly;

namespace JustSaying.UnitTests.Messaging.Policies.ExamplePolicies;

public class PollyMiddleware<TContext, TOut> : MiddlewareBase<TContext, TOut>
{
    private readonly ResiliencePipeline _pipeline;

    public PollyMiddleware(ResiliencePipeline pipeline)
    {
        _pipeline = pipeline;
    }

    protected override async Task<TOut> RunInnerAsync(
        TContext context,
        Func<CancellationToken, Task<TOut>> func,
        CancellationToken stoppingToken)
    {
        var pool = ResilienceContextPool.Shared;
        var resilienceContext = pool.Get(stoppingToken);

        try
        {
            return await _pipeline.ExecuteAsync(
                static async (context, func) =>
                    await func(context.CancellationToken), resilienceContext, func);
        }
        finally
        {
            pool.Return(resilienceContext);
        }
    }
}
