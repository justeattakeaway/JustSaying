using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Models;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenRegisteringTheSamePublisherTwice(ITestOutputHelper outputHelper) : GivenAServiceBus(outputHelper)
{
    private IMessagePublisher _publisher;

    protected override void Given()
    {
        base.Given();
        _publisher = Substitute.For<IMessagePublisher>();
        RecordAnyExceptionsThrown();
    }

    protected override Task WhenAsync()
    {
        SystemUnderTest.AddMessagePublisher<Message>(_publisher);
        SystemUnderTest.AddMessagePublisher<Message>(_publisher);

        return Task.CompletedTask;
    }

    [Fact]
    public void NoExceptionIsThrown()
    {
        // Specifying failover regions mean that messages can be registered more than once.
        ThrownException.ShouldBeNull();
    }

    [Fact]
    public void AndInterrogationShowsNonDuplicatedPublishers()
    {
        dynamic response = SystemUnderTest.Interrogate();

        Dictionary<string, InterrogationResult> publishedTypes = response.Data.PublishedMessageTypes;

        publishedTypes.ShouldContainKey(nameof(Message));
    }
}
