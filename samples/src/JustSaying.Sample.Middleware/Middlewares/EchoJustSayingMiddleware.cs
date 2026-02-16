using JustSaying.Messaging.Middleware;
using Microsoft.Extensions.Logging;

namespace JustSaying.Sample.Middleware.Middlewares;

/// <summary>
/// A middleware that will output a log message before and after a message has passed through it.
/// </summary>
public class EchoJustSayingMiddleware(string name) : MiddlewareBase<HandleMessageContext, bool>
{
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
    private static readonly ILogger Logger = LoggerFactory.CreateLogger<EchoJustSayingMiddleware>();

    protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
    {
        Logger.LogInformation("[{MiddlewareName}] Starting {Name} for {MessageType}", nameof(EchoJustSayingMiddleware), name, context.Message.GetType().Name);

        try
        {
            return await func(stoppingToken);
        }
        finally
        {
            Logger.LogInformation("[{MiddlewareName}] Ending {Name} for {MessageType}", nameof(EchoJustSayingMiddleware), name, context.Message.GetType().Name);
        }
    }
}
