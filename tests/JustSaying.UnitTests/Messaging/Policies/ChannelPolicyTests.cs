using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware.Receive;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using JustSaying.UnitTests.Messaging.Policies.ExamplePolicies;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.Messaging.Policies;

public class ChannelPolicyTests
{
    private IMessageReceivePauseSignal MessageReceivePauseSignal { get; set; }
    private ILoggerFactory LoggerFactory { get; set; }
    private IMessageMonitor MessageMonitor { get; set; }
    private TextWriter OutputHelper => TestContext.Current!.OutputWriter;

    [Before(Test)]
    public void Setup()
    {
        MessageReceivePauseSignal = new MessageReceivePauseSignal();
        LoggerFactory = OutputHelper.ToLoggerFactory();
        MessageMonitor = new TrackingLoggingMonitor(LoggerFactory.CreateLogger<TrackingLoggingMonitor>());
    }

    [Test]
    public async Task ErrorHandlingAroundSqs_WithCustomPolicy_CanSwallowExceptions()
    {
        // Arrange
        int queueCalledCount = 0;
        int dispatchedMessageCount = 0;
        var sqsQueue = TestQueue(() => Interlocked.Increment(ref queueCalledCount));

        var config = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(8);
        config.WithCustomMiddleware(
            new ErrorHandlingMiddleware<ReceiveMessagesContext, IList<Message>, InvalidOperationException>());

        var settings = new Dictionary<string, SubscriptionGroupConfigBuilder>
        {
            {
                "test", new SubscriptionGroupConfigBuilder("test").AddQueue(new SqsSource
                {
                    SqsQueue = sqsQueue,
                    MessageConverter = new InboundMessageConverter(SimpleMessage.Serializer, new MessageCompressionRegistry(), false)
                })
            }
        };

        IMessageDispatcher dispatcher = new FakeDispatcher(() => Interlocked.Increment(ref dispatchedMessageCount));

        var groupFactory = new SubscriptionGroupFactory(
            dispatcher,
            MessageReceivePauseSignal,
            MessageMonitor,
            LoggerFactory);

        ISubscriptionGroup collection = groupFactory.Create(config, settings);

        var cts = new CancellationTokenSource();
        var completion = collection.RunAsync(cts.Token);

        await Patiently.AssertThatAsync(OutputHelper,
            () =>
            {
                queueCalledCount.ShouldBeGreaterThan(1);
                dispatchedMessageCount.ShouldBe(0);
            });

        cts.Cancel();
        // Act and Assert

        await completion.HandleCancellation();
    }

    private static ISqsQueue TestQueue(Action spy = null)
    {
        IEnumerable<Message> GetMessages()
        {
            spy?.Invoke();
            throw new InvalidOperationException();
        }

        var queue = new FakeSqsQueue(ct => Task.FromResult(GetMessages()), "test-queue");

        return queue;
    }
}
