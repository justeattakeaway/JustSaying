using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenPublishingACollectionWithPublishAsync : GivenAServiceBus
{
    private readonly FakeLogCollector _logs = new();

    protected override void Given()
    {
        base.Given();
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(lf => lf.AddProvider(new FakeLoggerProvider(_logs)));
        RecordAnyExceptionsThrown();
    }

    protected override async Task WhenAsync()
    {
        SystemUnderTest.AddMessagePublisher<SimpleMessage>(Substitute.For<IMessagePublisher>());

        List<SimpleMessage> messages = [new SimpleMessage(), new SimpleMessage()];
        await SystemUnderTest.PublishAsync(messages);
    }

    [Test]
    public void TheExceptionPointsToPublishBatchAsync()
    {
        ThrownException.ShouldBeOfType<InvalidOperationException>().Message.ShouldContain("PublishBatchAsync");
    }

    [Test]
    public void TheErrorLogPointsToPublishBatchAsync()
    {
        var error = _logs.GetSnapshot().Where(record => record.Level == LogLevel.Error).ShouldHaveSingleItem();
        error.Message.ShouldContain("No publishers registered for message type");
        error.Message.ShouldContain("PublishBatchAsync");
    }
}
