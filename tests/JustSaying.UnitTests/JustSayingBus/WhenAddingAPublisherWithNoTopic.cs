using JustSaying.Messaging;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenAddingAPublisherWithNoTopic(ITestOutputHelper outputHelper) : GivenAServiceBus(outputHelper)
{
    protected override void Given()
    {
        RecordAnyExceptionsThrown();
    }

    protected override Task WhenAsync()
    {
        SystemUnderTest.AddMessagePublisher<SimpleMessage>(Substitute.For<IMessagePublisher>());

        return Task.CompletedTask;
    }

    [Fact]
    public void ExceptionThrown()
    {
        ThrownException.ShouldNotBeNull();
    }
}