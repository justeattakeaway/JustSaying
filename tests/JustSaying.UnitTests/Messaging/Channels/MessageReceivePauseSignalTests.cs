using JustSaying.Messaging.Channels.Receive;

namespace JustSaying.UnitTests.Messaging.Channels;

public class MessageReceivePauseSignalTests
{
    [Test]
    public void WhenInitialized_ReturnsIsPausedFalse()
    {
        var messageReceivePauseSignal = new MessageReceivePauseSignal();

        var result = messageReceivePauseSignal.IsPaused;

        result.ShouldBeFalse();
    }

    [Test]
    public void WhenPaused_ReturnsIsPaused()
    {
        var messageReceivePauseSignal = new MessageReceivePauseSignal();

        messageReceivePauseSignal.Pause();

        var result = messageReceivePauseSignal.IsPaused;

        result.ShouldBeTrue();
    }

    [Test]
    public void WhenStarted_ReturnsIsPausedFalse()
    {
        var messageReceivePauseSignal = new MessageReceivePauseSignal();

        messageReceivePauseSignal.Resume();

        var result = messageReceivePauseSignal.IsPaused;

        result.ShouldBeFalse();
    }
}
