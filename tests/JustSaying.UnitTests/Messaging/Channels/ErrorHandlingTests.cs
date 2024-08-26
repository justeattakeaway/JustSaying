using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.Messaging.Channels;

public class ErrorHandlingTests
{
    private IMessageReceivePauseSignal MessageReceivePauseSignal { get; }
    private ILoggerFactory LoggerFactory { get; }
    private IMessageMonitor MessageMonitor { get; }
    private readonly ITestOutputHelper _outputHelper;

    public ErrorHandlingTests(ITestOutputHelper testOutputHelper)
    {
        _outputHelper = testOutputHelper;
        MessageReceivePauseSignal = new MessageReceivePauseSignal();
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

        IMessageDispatcher dispatcher =
            new FakeDispatcher(() =>
            {
                Interlocked.Increment(ref messagesDispatched);
            });

        var defaults = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(8);
        var settings = new Dictionary<string, SubscriptionGroupConfigBuilder>
        {
            {
                "test", new SubscriptionGroupConfigBuilder("test").AddQueue(new SqsSource
                {
                    SqsQueue = queue,
                    MessageConverter = new ReceivedMessageConverter(new NewtonsoftMessageBodySerializer<SimpleMessage>(), new MessageCompressionRegistry([]))
                })
            }
        };

        var subscriptionGroupFactory = new SubscriptionGroupFactory(
            dispatcher,
            MessageReceivePauseSignal,
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

        IMessageDispatcher dispatcher =
            new FakeDispatcher(() => Interlocked.Increment(ref messagesDispatched));

        var defaults = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(1);
        var settings = new Dictionary<string, SubscriptionGroupConfigBuilder>
        {
            { "test", new SubscriptionGroupConfigBuilder("test").AddQueue(new SqsSource
            {
                SqsQueue = queue,
                MessageConverter = new ReceivedMessageConverter(new NewtonsoftMessageBodySerializer<SimpleMessage>(), new MessageCompressionRegistry([]))
            }) },
        };

        var subscriptionGroupFactory = new SubscriptionGroupFactory(
            dispatcher,
            MessageReceivePauseSignal,
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
