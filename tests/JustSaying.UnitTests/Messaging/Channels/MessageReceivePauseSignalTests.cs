using JustSaying.Messaging.Channels.Receive;

namespace JustSaying.UnitTests.Messaging.Channels;

public class MessageReceivePauseSignalTests
{
    [Fact]
    public void WhenInitialized_ReturnsIsPausedFalse()
    {
        var messageReceivePauseSignal = new MessageReceivePauseSignal();

        var result = messageReceivePauseSignal.IsPaused;

        Assert.False(result);
    }

    [Fact]
    public void WhenPaused_ReturnsIsPaused()
    {
        var messageReceivePauseSignal = new MessageReceivePauseSignal();

        messageReceivePauseSignal.Pause();

        var result = messageReceivePauseSignal.IsPaused;

        Assert.True(result);
    }

    [Fact]
    public void WhenStarted_ReturnsIsPausedFalse()
    {
        var messageReceivePauseSignal = new MessageReceivePauseSignal();

        messageReceivePauseSignal.Start();

        var result = messageReceivePauseSignal.IsPaused;

        Assert.False(result);
    }
}
