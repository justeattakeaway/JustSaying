using JustSaying.Messaging.Middleware;
using Serilog;

namespace JustSaying.Sample.Middleware.Middlewares;

/// <summary>
/// A middleware that will output a log message before and after a message has passed through it.
/// </summary>
public class EchoJustSayingMiddleware : MiddlewareBase<HandleMessageContext, bool>
{
    private readonly string _name;

    public EchoJustSayingMiddleware(string name)
    {
        _name = name;
    }

    protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
    {
        Log.Information("[{MiddlewareName}] Starting {Name} for {MessageType}", nameof(EchoJustSayingMiddleware), _name, context.Message.GetType().Name);

        try
        {
            return await func(stoppingToken);
        }
        finally
        {
            Log.Information("[{MiddlewareName}] Ending {Name} for {MessageType}", nameof(EchoJustSayingMiddleware), _name, context.Message.GetType().Name);
        }
    }
}
