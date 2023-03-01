using JustSaying.Messaging.MessageHandling;
using JustSaying.Sample.Middleware.Exceptions;
using JustSaying.Sample.Middleware.Messages;
using Serilog;

namespace JustSaying.Sample.Middleware.Handlers;

/// <summary>
/// A message handler that will randomly throw an exception, mimicking a transient error.
/// </summary>
public class UnreliableMessageHandler : IHandlerAsync<UnreliableMessage>
{
    public async Task<bool> Handle(UnreliableMessage message)
    {
        await Task.Delay(1000);

        if (Random.Shared.NextInt64() % 2 == 0)
        {
            Log.Information("Throwing for message id {MessageId}", message.Id);
            throw new BusinessException() { MessageId = message.Id.ToString() };
        }

        return true;
    }
}
