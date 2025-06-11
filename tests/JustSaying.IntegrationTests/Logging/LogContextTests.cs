using System.Text.RegularExpressions;
using JustSaying.IntegrationTests.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JustSaying.Logging;

public class LogContextTests(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
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

        var testLogger = sp.GetFakeLogCollector();
        var messageMatcher = new Regex(@"Published message ([a-zA-Z0-9\-]+) of type ([\w\.]+) to ([\w\s]+) '(.+?)'.");

        var handleMessage = testLogger.GetSnapshot()
            .Single(le => messageMatcher.IsMatch(le.Message));

        var propertyMap = handleMessage.StructuredState.ShouldNotBeNull().ToDictionary();
        propertyMap.ShouldContainKeyAndValue("MessageId", message.Id.ToString());
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

        var testLogger = sp.GetFakeLogCollector();
        var messageMatcher = new Regex(@"Published message ([a-zA-Z0-9\-]+) of type ([\w\.]+) to Queue '(.+?)'.");

        var handleMessage = testLogger.GetSnapshot()
            .Single(le => messageMatcher.IsMatch(le.Message));

        var propertyMap = handleMessage.StructuredState.ShouldNotBeNull().ToDictionary();
        propertyMap.ShouldContainKeyAndValue("MessageId", message.Id.ToString());
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

        var testLogger = sp.GetFakeLogCollector();
        var messageMatcher = new Regex(@"\w handling message with Id '([a-zA-Z0-9\-]+)' of type ([\w\.]+) in \d*ms.");

        await Patiently.AssertThatAsync(() =>
        {
            var handleMessage = testLogger.GetSnapshot()
                .SingleOrDefault(le => messageMatcher.IsMatch(le.Message));

            handleMessage.ShouldNotBeNull();

            handleMessage.Level.ShouldBe(level);

            if (exceptionMessage != null)
            {
                handleMessage.Exception.ShouldNotBeNull();
                handleMessage.Exception.Message.ShouldBe(exceptionMessage);
            }

            var propertyMap = handleMessage.StructuredState.ShouldNotBeNull().ToDictionary();
            propertyMap.ShouldContainKeyAndValue("Status", status);
            propertyMap.ShouldContainKeyAndValue("MessageId", message.Id.ToString());
            propertyMap.ShouldContainKeyAndValue("MessageType", message.GetType().FullName);
            propertyMap.ShouldContainKey("TimeToHandle");
        });
        cts.Cancel();
    }
}
