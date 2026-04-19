using JustSaying.Messaging;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenAddingAPublisherWithNoTopic : GivenAServiceBus
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

    [Test]
    public void ExceptionThrown()
    {
        ThrownException.ShouldNotBeNull();
    }
}