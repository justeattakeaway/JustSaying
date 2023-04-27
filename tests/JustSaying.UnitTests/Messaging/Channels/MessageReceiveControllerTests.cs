using JustSaying.Messaging.Channels.Receive;

namespace JustSaying.UnitTests.Messaging.Channels;

public class MessageReceiveControllerTests
{
    [Fact]
    public void WhenInitialized_ReturnsFalse()
    {
        var messageReceiveController = new MessageReceiveController();

        var result = messageReceiveController.Stopped();

        Assert.False(result);
    }

    [Fact]
    public void WhenStopped_ReturnsTrue()
    {
        var messageReceiveController = new MessageReceiveController();

        messageReceiveController.Stop();

        var result = messageReceiveController.Stopped();

        Assert.True(result);
    }

    [Fact]
    public void WhenStarted_ReturnsFalse()
    {
        var messageReceiveController = new MessageReceiveController();

        messageReceiveController.Start();

        var result = messageReceiveController.Stopped();

        Assert.False(result);
    }
}
