using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Models;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenRegisteringTheSamePublisherTwice : GivenAServiceBus
{
    private IMessagePublisher<Message> _publisher;

    protected override void Given()
    {
        base.Given();
        _publisher = Substitute.For<IMessagePublisher<Message>>();
        RecordAnyExceptionsThrown();
    }

    protected override Task WhenAsync()
    {
        SystemUnderTest.AddMessagePublisher(_publisher);
        SystemUnderTest.AddMessagePublisher(_publisher);

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

    public WhenRegisteringTheSamePublisherTwice(ITestOutputHelper outputHelper) : base(outputHelper)
    { }
}
