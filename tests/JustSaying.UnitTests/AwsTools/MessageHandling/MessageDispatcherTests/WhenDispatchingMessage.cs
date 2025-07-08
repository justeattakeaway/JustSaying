using System.Globalization;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.Backoff;
using JustSaying.Models;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.Fakes;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Newtonsoft.Json;
using NSubstitute;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.MessageDispatcherTests;

public class WhenDispatchingMessage : IAsyncLifetime
{
    private const string ExpectedQueueUrl = "http://testurl.com/queue";

    private readonly TrackingLoggingMonitor _messageMonitor;
    private readonly MiddlewareMap _middlewareMap;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ITestOutputHelper _outputHelper;

    private FakeSqsQueue _queue;
    private SQSMessage _sqsMessage;
    private Message _typedMessage = new SimpleMessage();

    internal MessageDispatcher SystemUnderTest { get; private set; }

    private readonly IMessageBackoffStrategy _messageBackoffStrategy = Substitute.For<IMessageBackoffStrategy>();
    private readonly IMessageBodySerializer _messageBodySerializer = Substitute.For<IMessageBodySerializer>();
    private readonly InboundMessageConverter _messageConverter;
    private readonly FakeLogCollector _fakeLogCollector;

    public WhenDispatchingMessage(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        var services =
            new ServiceCollection().AddLogging(lb =>
            {
                lb.SetMinimumLevel(LogLevel.Information);
                lb.AddFakeLogging().AddXUnit(outputHelper);
            });
        var sp =  services.BuildServiceProvider();
        _fakeLogCollector = sp.GetFakeLogCollector();
        _loggerFactory = sp.GetService<ILoggerFactory>();
        _messageMonitor = new TrackingLoggingMonitor(_loggerFactory.CreateLogger<TrackingLoggingMonitor>());
        _middlewareMap = new MiddlewareMap();
        _messageConverter = new InboundMessageConverter(_messageBodySerializer, new MessageCompressionRegistry(), true);
    }

    public virtual async ValueTask InitializeAsync()
    {
        Given();

        SystemUnderTest = CreateSystemUnderTestAsync();

        await When();
    }

    public virtual ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected virtual void Given()
    {
        _typedMessage = new SimpleMessage();

        _sqsMessage = new SQSMessage
        {
            Body = JsonConvert.SerializeObject(_typedMessage),
            ReceiptHandle = "i_am_receipt_handle"
        };

        IEnumerable<SQSMessage> GetMessages()
        {
            yield return _sqsMessage;
        }

        _queue = new FakeSqsQueue(ct => Task.FromResult(GetMessages()))
        {
            Uri = new Uri(ExpectedQueueUrl),
            MaxNumberOfMessagesToReceive = 1
        };

        _messageBodySerializer.Deserialize(Arg.Any<string>()).Returns(_typedMessage);
    }

    private async Task When()
    {
        var queueReader = new SqsQueueReader(_queue, _messageConverter);
        await SystemUnderTest.DispatchMessageAsync(queueReader.ToMessageContext(_sqsMessage), CancellationToken.None);
    }

    private MessageDispatcher CreateSystemUnderTestAsync()
    {
        var dispatcher = new MessageDispatcher(
            _messageMonitor,
            _middlewareMap,
            _loggerFactory);

        return dispatcher;
    }

    [Fact]
    public void ShouldDeserializeMessage()
    {
        _messageBodySerializer.Received(1).Deserialize(Arg.Is<string>(x => x == _sqsMessage.Body));
    }

    public class AndHandlerMapDoesNotHaveMatchingHandler(ITestOutputHelper outputHelper) : WhenDispatchingMessage(outputHelper)
    {
        private const int ExpectedReceiveCount = 1;
        private readonly TimeSpan _expectedBackoffTimeSpan = TimeSpan.FromMinutes(4);

        protected override void Given()
        {
            base.Given();
            _sqsMessage.Attributes ??= [];
            _sqsMessage.Attributes.Add(MessageSystemAttributeName.ApproximateReceiveCount, ExpectedReceiveCount.ToString(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ShouldNotHandleMessage()
        {
            _messageBodySerializer.Received(1).Deserialize(Arg.Is<string>(x => x == _sqsMessage.Body));
            _messageMonitor.HandledMessages.ShouldBeEmpty();
            await Patiently.AssertThatAsync(_outputHelper, () =>
            {
                var logs = _fakeLogCollector.GetSnapshot();
                logs.ShouldContain(le => le.Message == "Failed to dispatch. Middleware for message of type 'JustSaying.TestingFramework.SimpleMessage' not found in middleware map.");
            });
        }
    }

    public class AndMessageProcessingSucceeds(ITestOutputHelper outputHelper) : WhenDispatchingMessage(outputHelper)
    {
        protected override void Given()
        {
            base.Given();

            var handler = new InspectableHandler<SimpleMessage>();

            var testResolver = new InMemoryServiceResolver(_outputHelper,
                _messageMonitor,
                sc => sc.AddSingleton<IHandlerAsync<SimpleMessage>>(handler));

            var middleware = new HandlerMiddlewareBuilder(testResolver, testResolver)
                .UseBackoff(_messageBackoffStrategy)
                .UseDefaults<SimpleMessage>(handler.GetType())
                .Build();

            _middlewareMap.Add<SimpleMessage>(_queue.QueueName, middleware);
        }

        [Fact]
        public void ShouldDeleteMessageIfHandledSuccessfully()
        {
            var request = _queue.DeleteMessageRequests.ShouldHaveSingleItem();
            request.QueueUrl.ShouldBe(ExpectedQueueUrl);
            request.ReceiptHandle.ShouldBe(_sqsMessage.ReceiptHandle);
        }
    }

    public class AndMessageProcessingFails(ITestOutputHelper outputHelper) : WhenDispatchingMessage(outputHelper)
    {
        private const int ExpectedReceiveCount = 1;
        private readonly TimeSpan _expectedBackoffTimeSpan = TimeSpan.FromMinutes(4);
        private readonly Exception _expectedException = new("Something failed when processing");

        protected override void Given()
        {
            base.Given();

            var handler = new InspectableHandler<SimpleMessage>()
            {
                OnHandle = msg => throw _expectedException
            };

            var testResolver = new InMemoryServiceResolver(_outputHelper,
                _messageMonitor,
                sc => sc.AddSingleton<IHandlerAsync<SimpleMessage>>(handler));

            var middleware = new HandlerMiddlewareBuilder(testResolver, testResolver)
                .UseBackoff(_messageBackoffStrategy)
                .UseDefaults<SimpleMessage>(handler.GetType())
                .Build();

            _middlewareMap.Add<SimpleMessage>(_queue.QueueName, middleware);

            _messageBackoffStrategy.GetBackoffDuration(_typedMessage, 1, _expectedException).Returns(_expectedBackoffTimeSpan);
            _sqsMessage.Attributes ??= [];
            _sqsMessage.Attributes.Add(MessageSystemAttributeName.ApproximateReceiveCount, ExpectedReceiveCount.ToString(CultureInfo.InvariantCulture));
        }

        [Fact]
        public void ShouldInvokeMessageBackoffStrategyWithNumberOfReceives()
        {
            _messageBackoffStrategy.Received(1).GetBackoffDuration(Arg.Is(_typedMessage), Arg.Is(ExpectedReceiveCount), Arg.Is(_expectedException));
        }

        [Fact]
        public void ShouldUpdateMessageVisibility()
        {
            var request = _queue.ChangeMessageVisibilityRequests.ShouldHaveSingleItem();
            request.QueueUrl.ShouldBe(ExpectedQueueUrl);
            request.ReceiptHandle.ShouldBe(_sqsMessage.ReceiptHandle);
            request.VisibilityTimeoutInSeconds.ShouldBe((int)_expectedBackoffTimeSpan.TotalSeconds);
        }
    }
}
