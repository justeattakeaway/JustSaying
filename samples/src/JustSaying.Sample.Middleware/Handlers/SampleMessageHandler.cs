using JustSaying.Messaging.MessageHandling;
using JustSaying.Sample.Middleware.Messages;

namespace JustSaying.Sample.Middleware.Handlers;

/// <summary>
/// A message handler that will always return true.
/// </summary>
public class SampleMessageHandler : IHandlerAsync<SampleMessage>
{
    public async Task<bool> Handle(SampleMessage message)
    {
        await Task.Delay(1000);
        return true;
    }
}
