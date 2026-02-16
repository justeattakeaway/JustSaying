using JustSaying.Messaging.MessageHandling;
using JustSaying.Sample.Middleware.Exceptions;
using JustSaying.Sample.Middleware.Messages;
using Microsoft.Extensions.Logging;

namespace JustSaying.Sample.Middleware.Handlers;

/// <summary>
/// A message handler that will randomly throw an exception, mimicking a transient error.
/// </summary>
public class UnreliableMessageHandler : IHandlerAsync<UnreliableMessage>
{
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
    private static readonly ILogger Logger = LoggerFactory.CreateLogger<UnreliableMessageHandler>();

    public async Task<bool> Handle(UnreliableMessage message)
    {
        await Task.Delay(1000);

        if (Random.Shared.NextInt64() % 2 == 0)
        {
            Logger.LogInformation("Throwing for message id {MessageId}", message.Id);
            throw new BusinessException() { MessageId = message.Id.ToString() };
        }

        return true;
    }
}
