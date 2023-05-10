using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.Messaging.Channels;

public class ErrorHandlingTests
{
    private IMessageReceiveStatusSetter MessageReceiveStatusSetter { get; }
    private ILoggerFactory LoggerFactory { get; }
    private IMessageMonitor MessageMonitor { get; }
    private readonly ITestOutputHelper _outputHelper;

    public ErrorHandlingTests(ITestOutputHelper testOutputHelper)
    {
        _outputHelper = testOutputHelper;
        MessageReceiveStatusSetter = new MessageReceiveStatusSetter();
        LoggerFactory = testOutputHelper.ToLoggerFactory();
        MessageMonitor = new TrackingLoggingMonitor(LoggerFactory.CreateLogger<TrackingLoggingMonitor>());
    }

    [Fact]
    public async Task Sqs_Client_Throwing_Exceptions_Continues_To_Request_Messages()
    {
        // Arrange
        int messagesRequested = 0;
        int messagesDispatched = 0;

        IEnumerable<Message> GetMessages()
        {
            Interlocked.Increment(ref messagesRequested);
            throw new Exception();
        }
        var queue = new FakeSqsQueue(ct => Task.FromResult(GetMessages()));


        var queues = new List<ISqsQueue> { queue };
        IMessageDispatcher dispatcher =
            new FakeDispatcher(() =>
            {
                Interlocked.Increment(ref messagesDispatched);
            });

        var defaults = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(8);
        var settings = new Dictionary<string, SubscriptionGroupConfigBuilder>
        {
            { "test", new SubscriptionGroupConfigBuilder("test").AddQueues(queues) },
        };

        var subscriptionGroupFactory = new SubscriptionGroupFactory(
            dispatcher,
            MessageReceiveStatusSetter,
            MessageMonitor,
            LoggerFactory);

        ISubscriptionGroup collection = subscriptionGroupFactory.Create(defaults, settings);

        var cts = new CancellationTokenSource();

        // Act
        var runTask = collection.RunAsync(cts.Token);

        await Patiently.AssertThatAsync(_outputHelper,
            () =>
            {
                messagesRequested.ShouldBeGreaterThan(1, $"but was {messagesRequested}");
                messagesDispatched.ShouldBe(0, $"but was {messagesDispatched}");
            });

        cts.Cancel();
        await runTask.HandleCancellation();
    }

    [Fact]
    public async Task Message_Processing_Throwing_Exceptions_Continues_To_Request_Messages()
    {
        // Arrange
        int messagesRequested = 0;
        int messagesDispatched = 0;

        IEnumerable<Message> GetMessages()
        {
            Interlocked.Increment(ref messagesRequested);
            throw new Exception();
        }
        var queue = new FakeSqsQueue(ct => Task.FromResult(GetMessages()));

        var queues = new List<ISqsQueue> { queue };
        IMessageDispatcher dispatcher =
            new FakeDispatcher(() => Interlocked.Increment(ref messagesDispatched));

        var defaults = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(1);
        var settings = new Dictionary<string, SubscriptionGroupConfigBuilder>
        {
            { "test", new SubscriptionGroupConfigBuilder("test").AddQueues(queues) },
        };

        var subscriptionGroupFactory = new SubscriptionGroupFactory(
            dispatcher,
            MessageReceiveStatusSetter,
            MessageMonitor,
            LoggerFactory);

        ISubscriptionGroup collection = subscriptionGroupFactory.Create(defaults, settings);

        var cts = new CancellationTokenSource();

        // Act
        var runTask = collection.RunAsync(cts.Token);

        await Patiently.AssertThatAsync(_outputHelper,
            () =>
            {
                messagesRequested.ShouldBeGreaterThan(1);
                messagesDispatched.ShouldBe(0);
            });

        cts.Cancel();
        await runTask.HandleCancellation();
    }
}
