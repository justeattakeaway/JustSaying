using JustSaying.Messaging.MessageHandling;

namespace JustSaying.UnitTests.Messaging.Channels.TestHelpers;

public class TestHandler<T> : IHandlerAsync<T>
{
    private readonly Action<T> _spy;

    public TestHandler(Action<T> spy)
    {
        _spy = spy;
    }

    public async Task<bool> Handle(T testMessage)
    {
        _spy?.Invoke(testMessage);

        await Task.Delay(100);
        return true;
    }
}