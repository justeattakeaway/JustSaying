using JustSaying.Messaging.Middleware;
using JustSaying.Sample.Middleware.Messages;
using Microsoft.Extensions.Logging;

namespace JustSaying.Sample.Middleware.Middlewares;

/// <summary>
/// A middleware that will interrogate each message and output some information.
/// </summary>
public sealed class InterrogateMiddleware : MiddlewareBase<HandleMessageContext, bool>
{
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
    private static readonly ILogger Logger = LoggerFactory.CreateLogger<InterrogateMiddleware>();

    protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
    {
        if (context.Message is SampleMessage simpleMessage)
        {
            Logger.LogInformation("[{MiddlewareName}] Hello SampleMessage! Your SampleId is {SampleId} and Id is {Id}", nameof(InterrogateMiddleware), simpleMessage.SampleId, simpleMessage.Id);
        }
        else if (context.Message is UnreliableMessage unreliableMessage)
        {
            Logger.LogInformation("[{MiddlewareName}] Hello UnreliableMessage! Hope you work this time....your Name is {Name} and Id is {Id}", nameof(InterrogateMiddleware),unreliableMessage.Name, context.Message.Id);
        }

        return await func(stoppingToken);
    }
}
