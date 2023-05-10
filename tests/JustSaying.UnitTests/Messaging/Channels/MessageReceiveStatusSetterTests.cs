using JustSaying.Messaging.Channels.Receive;

namespace JustSaying.UnitTests.Messaging.Channels;

public class MessageReceiveStatusSetterTests
{
    [Fact]
    public void WhenInitialized_ReturnsStatusReceiving()
    {
        var messageReceiveStatusSetter = new MessageReceiveStatusSetter();

        var result = messageReceiveStatusSetter.Status;

        Assert.Equal(MessageReceiveStatus.Receiving, result);
    }

    [Fact]
    public void WhenStopped_ReturnsStatusNotReceiving()
    {
        var messageReceiveStatusSetter = new MessageReceiveStatusSetter();

        messageReceiveStatusSetter.Stop();

        var result = messageReceiveStatusSetter.Status;

        Assert.Equal(MessageReceiveStatus.NotReceiving, result);
    }

    [Fact]
    public void WhenStarted_ReturnsStatusReceiving()
    {
        var messageReceiveStatusSetter = new MessageReceiveStatusSetter();

        messageReceiveStatusSetter.Start();

        var result = messageReceiveStatusSetter.Status;

        Assert.Equal(MessageReceiveStatus.Receiving, result);
    }
}
