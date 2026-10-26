using JustSaying.Messaging;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenPublishingNullMessages : GivenAServiceBus
{
    protected override Task WhenAsync()
    {
        SystemUnderTest.AddMessagePublisher<SimpleMessage>(Substitute.For<IMessagePublisher, IMessageBatchPublisher>());
        return Task.CompletedTask;
    }

    [Test]
    public async Task PublishingANullMessageThrowsArgumentNullException()
    {
        var exception = await Should.ThrowAsync<ArgumentNullException>(
            () => SystemUnderTest.PublishAsync<SimpleMessage>(null, CancellationToken.None));

        exception.ParamName.ShouldBe("message");
    }

    [Test]
    public async Task PublishingANullBatchThrowsArgumentNullException()
    {
        var exception = await Should.ThrowAsync<ArgumentNullException>(
            () => SystemUnderTest.PublishBatchAsync<SimpleMessage>(null, null, CancellationToken.None));

        exception.ParamName.ShouldBe("messages");
    }

    [Test]
    public async Task PublishingABatchContainingNullThrowsArgumentException()
    {
        var exception = await Should.ThrowAsync<ArgumentException>(
            () => SystemUnderTest.PublishBatchAsync([new SimpleMessage(), null], null, CancellationToken.None));

        exception.ParamName.ShouldBe("messages");
    }
}
