using JustSaying.Messaging.Middleware;
using JustSaying.Models;
using JustSaying.Sample.Middleware.Messages;
using Serilog;

namespace JustSaying.Sample.Middleware.Middlewares;

/// <summary>
/// A middleware that will interrogate each message and output some information.
/// </summary>
public sealed class InterrogateMiddleware : MiddlewareBase<HandleMessageContext, bool>
{
    protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
    {
        if (context.Message is SampleMessage simpleMessage)
        {
            Log.Information("[{MiddlewareName}] Hello SampleMessage! Your SampleId is {SampleId} and Id is {Id}", nameof(InterrogateMiddleware), simpleMessage.SampleId, simpleMessage.Id);
        }
        else if (context.Message is UnreliableMessage unreliableMessage)
        {
            var messageId = (context.Message as Message)?.Id.ToString() ?? "<unknown>";
            Log.Information("[{MiddlewareName}] Hello UnreliableMessage! Hope you work this time....your Name is {Name} and Id is {Id}", nameof(InterrogateMiddleware),unreliableMessage.Name, messageId);
        }

        return await func(stoppingToken);
    }
}
