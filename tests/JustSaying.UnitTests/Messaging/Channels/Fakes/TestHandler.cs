using JustSaying.Messaging.MessageHandling;

namespace JustSaying.UnitTests.Messaging.Channels.TestHelpers;

public class TestHandler<T>(Action<T> spy) : IHandlerAsync<T>
{
    public async Task<bool> Handle(T testMessage)
    {
        spy?.Invoke(testMessage);

        await Task.Delay(100);
        return true;
    }
}
