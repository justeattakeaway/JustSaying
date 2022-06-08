using JustSaying.IntegrationTests.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using MELT;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JustSaying.Logging;

public class LogContextTests : IntegrationTestBase
{
    public LogContextTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }


    [AwsFact]
    public async Task PublishToTopicLogsShouldHaveContext()
    {
        var services = GivenJustSaying(levelOverride: LogLevel.Information)
            .ConfigureJustSaying(
                (builder) => builder.WithLoopbackTopic<SimpleMessage>(UniqueName));

        var sp = services.BuildServiceProvider();

        var cts = new CancellationTokenSource();

        var publisher = sp.GetRequiredService<IMessagePublisher>();
        await publisher.StartAsync(cts.Token);

        var message = new SimpleMessage();
        await publisher.PublishAsync(message, cts.Token);

        var testLogger = sp.GetRequiredService<ITestLoggerSink>();

        var handleMessage = testLogger.LogEntries
            .Single(le => le.OriginalFormat == "Published message {MessageId} of type {MessageType} to {DestinationType} '{MessageDestination}'.");

        var propertyMap = new Dictionary<string, object>(handleMessage.Properties);
        propertyMap.ShouldContainKeyAndValue("MessageId", message.Id);
        propertyMap.ShouldContainKeyAndValue("MessageType", message.GetType().FullName);
        propertyMap.ShouldContainKeyAndValue("DestinationType", "Topic");
        propertyMap.ShouldContainKey("MessageDestination");

        cts.Cancel();
    }

    [AwsFact]
    public async Task PublishToQueueLogsShouldHaveContext()
    {
        var services = GivenJustSaying(levelOverride: LogLevel.Information)
            .ConfigureJustSaying(
                (builder) => builder.WithLoopbackQueue<SimpleMessage>(UniqueName));

        var sp = services.BuildServiceProvider();

        var cts = new CancellationTokenSource();

        var publisher = sp.GetRequiredService<IMessagePublisher>();
        await publisher.StartAsync(cts.Token);

        var message = new SimpleMessage();
        await publisher.PublishAsync(message, cts.Token);

        var testLogger = sp.GetRequiredService<ITestLoggerSink>();

        var handleMessage = testLogger.LogEntries
            .Single(le => le.OriginalFormat == "Published message {MessageId} of type {MessageType} to {DestinationType} '{MessageDestination}'.");

        var propertyMap = new Dictionary<string, object>(handleMessage.Properties);
        propertyMap.ShouldContainKeyAndValue("MessageId", message.Id);
        propertyMap.ShouldContainKeyAndValue("MessageType", message.GetType().FullName);
        propertyMap.ShouldContainKeyAndValue("DestinationType", "Queue");
        propertyMap.ShouldContainKey("MessageDestination");

        cts.Cancel();
    }

    [AwsTheory]
    [InlineData(true, LogLevel.Information, "Succeeded", null)]
    [InlineData(false, LogLevel.Warning, "Failed", null)]
    [InlineData(false, LogLevel.Warning, "Failed", "Something went wrong!")]
    public async Task HandleMessageFromQueueLogs_ShouldHaveContext(bool handlerShouldSucceed, LogLevel level, string status, string exceptionMessage)
    {
        var handler = new InspectableHandler<SimpleMessage>()
        {
            ShouldSucceed = handlerShouldSucceed,
        };
        if (exceptionMessage != null)
        {
            handler.OnHandle = msg => throw new Exception(exceptionMessage);
        }

        var services = GivenJustSaying(levelOverride: LogLevel.Information)
            .ConfigureJustSaying(
                (builder) => builder.WithLoopbackQueue<SimpleMessage>(UniqueName))
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        var sp = services.BuildServiceProvider();

        var cts = new CancellationTokenSource();

        var publisher = sp.GetRequiredService<IMessagePublisher>();
        await publisher.StartAsync(cts.Token);
        await sp.GetRequiredService<IMessagingBus>().StartAsync(cts.Token);

        var message = new SimpleMessage();
        await publisher .PublishAsync(message, cts.Token);

        await Patiently.AssertThatAsync(() => handler.ReceivedMessages
            .ShouldHaveSingleItem()
            .Id.ShouldBe(message.Id));

        var testLogger = sp.GetRequiredService<ITestLoggerSink>();

        await Patiently.AssertThatAsync(() =>
        {
            var handleMessage = testLogger.LogEntries
                .SingleOrDefault(le => le.OriginalFormat == "{Status} handling message with Id '{MessageId}' of type {MessageType} in {TimeToHandle}ms.");

            handleMessage.ShouldNotBeNull();

            handleMessage.LogLevel.ShouldBe(level);
            Assert.Equal(exceptionMessage, handleMessage.Exception?.Message);

            var propertyMap = new Dictionary<string, object>(handleMessage.Properties);
            propertyMap.ShouldContainKeyAndValue("Status", status);
            propertyMap.ShouldContainKeyAndValue("MessageId", message.Id);
            propertyMap.ShouldContainKeyAndValue("MessageType", message.GetType().FullName);
            propertyMap.ShouldContainKey("TimeToHandle");
        });
        cts.Cancel();
    }
}
