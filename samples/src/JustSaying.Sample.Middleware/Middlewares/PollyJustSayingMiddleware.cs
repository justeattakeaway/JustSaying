using JustSaying.Messaging.Middleware;
using JustSaying.Sample.Middleware.Exceptions;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace JustSaying.Sample.Middleware.Middlewares;

/// <summary>
/// A middleware that will wrap the handling of a message in the provided Polly policy.
/// </summary>
public class PollyJustSayingMiddleware : MiddlewareBase<HandleMessageContext, bool>
{
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
    private static readonly ILogger Logger = LoggerFactory.CreateLogger<PollyJustSayingMiddleware>();

    private readonly ResiliencePipeline _pipeline;

    public PollyJustSayingMiddleware()
    {
        _pipeline = CreateResiliencePipeline();
    }

    protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
    {
        Logger.LogInformation("[{MiddlewareName}] Started {MessageType}", nameof(PollyJustSayingMiddleware), context.Message.GetType().Name);

        try
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
        finally
        {
            Logger.LogInformation("[{MiddlewareName}] Finished {MessageType}", nameof(PollyJustSayingMiddleware), context.Message.GetType().Name);
        }
    }

    private static ResiliencePipeline CreateResiliencePipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<BusinessException>(),
                Delay = TimeSpan.FromSeconds(1),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = (args) =>
                {
                    Logger.LogInformation("Retrying failed operation on count {RetryCount}", args.AttemptNumber);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
