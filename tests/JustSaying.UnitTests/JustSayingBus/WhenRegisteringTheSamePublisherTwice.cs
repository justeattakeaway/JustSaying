using JustSaying.Messaging;
using JustSaying.Models;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenRegisteringTheSamePublisherTwice : GivenAServiceBus
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

        string[] publishedTypes = response.Data.PublishedMessageTypes;

        publishedTypes.ShouldContain(nameof(Message));
    }

    public WhenRegisteringTheSamePublisherTwice(ITestOutputHelper outputHelper) : base(outputHelper)
    { }
}