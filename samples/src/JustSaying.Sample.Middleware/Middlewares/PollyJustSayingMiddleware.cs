using JustSaying.Messaging.Middleware;
using JustSaying.Sample.Middleware.Exceptions;
using Polly;
using Serilog;

namespace JustSaying.Sample.Middleware.Middlewares;

/// <summary>
/// A middleware that will wrap the handling of a message in the provided Polly policy.
/// </summary>
public class PollyJustSayingMiddleware : MiddlewareBase<HandleMessageContext, bool>
{
    private readonly AsyncPolicy _policy;

    public PollyJustSayingMiddleware()
    {
        _policy = CreateMessageRetryPolicy();
    }

    protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
    {
        Log.Information("[{MiddlewareName}] Started {MessageType}", nameof(PollyJustSayingMiddleware), context.Message.GetType().Name);

        try
        {
            return await _policy.ExecuteAsync(func, stoppingToken);
        }
        finally
        {
            Log.Information("[{MiddlewareName}] Finished {MessageType}", nameof(PollyJustSayingMiddleware), context.Message.GetType().Name);
        }
    }

    private static AsyncPolicy CreateMessageRetryPolicy()
    {
        return Policy.Handle<BusinessException>()
            .WaitAndRetryAsync(3, count => TimeSpan.FromMilliseconds(Math.Max(count * 100, 1000)),
                onRetry: (e, ts, retryCount, ctx) => Log.Information(e, "Retrying failed operation on count {RetryCount}", retryCount));
    }
}
