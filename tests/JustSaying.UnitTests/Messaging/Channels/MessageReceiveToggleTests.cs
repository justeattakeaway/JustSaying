using JustSaying.Messaging.Channels.Receive;

namespace JustSaying.UnitTests.Messaging.Channels;

public class MessageReceiveToggleTests
{
    [Fact]
    public void WhenInitialized_ReturnsStatusReceiving()
    {
        var messageReceiveToggle = new MessageReceiveToggle();

        var result = messageReceiveToggle.Status;

        Assert.Equal(MessageReceiveStatus.Receiving, result);
    }

    [Fact]
    public void WhenStopped_ReturnsStatusNotReceiving()
    {
        var messageReceiveToggle = new MessageReceiveToggle();

        messageReceiveToggle.Stop();

        var result = messageReceiveToggle.Status;

        Assert.Equal(MessageReceiveStatus.NotReceiving, result);
    }

    [Fact]
    public void WhenStarted_ReturnsStatusReceiving()
    {
        var messageReceiveToggle = new MessageReceiveToggle();

        messageReceiveToggle.Start();

        var result = messageReceiveToggle.Status;

        Assert.Equal(MessageReceiveStatus.Receiving, result);
    }
}
