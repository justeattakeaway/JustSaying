using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using Microsoft.Extensions.Logging;
using NSubstitute;
using HandleMessageMiddleware = JustSaying.Messaging.Middleware.MiddlewareBase<JustSaying.Messaging.Middleware.HandleMessageContext, bool>;

namespace JustSaying.UnitTests.Messaging.Channels;

public class SubscriptionGroupCollectionTests
{
    private IMessageReceiveToggle MessageReceiveToggle { get; }
    private ILoggerFactory LoggerFactory { get; }
    private IMessageMonitor MessageMonitor { get; }
    private readonly ITestOutputHelper _outputHelper;


    public SubscriptionGroupCollectionTests(ITestOutputHelper testOutputHelper)
    {
        MessageReceiveToggle = new MessageReceiveToggle();
        _outputHelper = testOutputHelper;
        LoggerFactory = testOutputHelper.ToLoggerFactory();
        MessageMonitor = new TrackingLoggingMonitor(LoggerFactory.CreateLogger<TrackingLoggingMonitor>());
    }

    [Fact]
    public async Task Add_Different_Handler_Per_Queue()
    {
        // Arrange
        string group1 = "group1";
        string group2 = "group2";
        string queueName1 = "queue1";
        string queueName2 = "queue2";

        JustSaying.JustSayingBus bus = CreateBus();

        var middleware1 = new InspectableMiddleware<TestJustSayingMessage>();
        var middleware2 = new InspectableMiddleware<TestJustSayingMessage>();

        bus.AddMessageMiddleware<TestJustSayingMessage>(queueName1, middleware1);
        bus.AddMessageMiddleware<TestJustSayingMessage>(queueName2, middleware2);

        ISqsQueue queue1 = TestQueue(bus.SerializationRegister, queueName1);
        ISqsQueue queue2 = TestQueue(bus.SerializationRegister, queueName2);

        bus.AddQueue(group1, queue1);
        bus.AddQueue(group2, queue2);

        using var cts = new CancellationTokenSource();

        // Act
        await bus.StartAsync(cts.Token);

        await Patiently.AssertThatAsync(_outputHelper,
            () =>
            {
                middleware1.Handler.ReceivedMessages.Count.ShouldBeGreaterThan(0);
                middleware2.Handler.ReceivedMessages.Count.ShouldBeGreaterThan(0);
            });

        cts.Cancel();
        await bus.Completion;

        foreach (var message in middleware1.Handler.ReceivedMessages)
        {
            message.QueueName.ShouldBe(queueName1);
        }

        foreach (var message in middleware2.Handler.ReceivedMessages)
        {
            message.QueueName.ShouldBe(queueName2);
        }

        bus.Dispose();
    }

    private JustSaying.JustSayingBus CreateBus()
    {
        var config = Substitute.For<IMessagingConfig>();
        var serializationRegister = new MessageSerializationRegister(
            new NonGenericMessageSubjectProvider(),
            new NewtonsoftSerializationFactory());

        var bus = new JustSaying.JustSayingBus(config, serializationRegister, MessageReceiveToggle, LoggerFactory, MessageMonitor);

        var defaultSubscriptionSettings = new SubscriptionGroupSettingsBuilder()
            .WithDefaultMultiplexerCapacity(1)
            .WithDefaultPrefetch(1)
            .WithDefaultBufferSize(1)
            .WithDefaultConcurrencyLimit(1);

        bus.SetGroupSettings(defaultSubscriptionSettings, new Dictionary<string, SubscriptionGroupConfigBuilder>());

        return bus;
    }

    private static ISqsQueue TestQueue(
        IMessageSerializationRegister messageSerializationRegister,
        string queueName,
        Action spy = null)
    {
        var message = new TestJustSayingMessage
        {
            QueueName = queueName,
        };

        var messages = new List<Message>
        {
            new TestMessage { Body = messageSerializationRegister.Serialize(message, false) },
        };

        var queue = new FakeSqsQueue(async ct =>
        {
            spy?.Invoke();
            await Task.Delay(30, ct);
            return messages;
        }, queueName);

        return queue;
    }

    private class TestMessage : Message
    {
        public override string ToString()
        {
            return Body;
        }
    }

    private class TestJustSayingMessage : JustSaying.Models.Message
    {
        public string QueueName { get; set; }

        public override string ToString()
        {
            return QueueName;
        }
    }
}
