using JustSaying.Messaging.Middleware;

namespace JustSaying.UnitTests.Messaging.Policies.ExamplePolicies;

public class ErrorHandlingMiddleware<TContext, TOut, TException> : MiddlewareBase<TContext, TOut>
    where TException : Exception
{
    protected override async Task<TOut> RunInnerAsync(
        TContext context,
        Func<CancellationToken, Task<TOut>> func,
        CancellationToken stoppingToken)
    {
        try
        {
            return await func(stoppingToken);
        }
        catch (TException)
        {
            return default;
        }
    }
}