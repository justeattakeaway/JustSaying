using System.Globalization;
using Amazon.SQS;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Compression;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.Backoff;
using JustSaying.Models;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.Fakes;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using MELT;
using Meziantou.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.MessageDispatcherTests;

[DisableParallelization]
public class WhenDispatchingMessage : IAsyncLifetime
{
    private const string ExpectedQueueUrl = "http://testurl.com/queue";

    private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
    private readonly TrackingLoggingMonitor _messageMonitor;
    private readonly MiddlewareMap _middlewareMap;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ITestOutputHelper _outputHelper;

    private FakeSqsQueue _queue;
    private SQSMessage _sqsMessage;
    private Message _typedMessage = new SimpleMessage();

    internal MessageDispatcher SystemUnderTest { get; private set; }

    protected readonly IMessageBackoffStrategy MessageBackoffStrategy = Substitute.For<IMessageBackoffStrategy>();

    public WhenDispatchingMessage(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _loggerFactory = TestLoggerFactory.Create(lb => lb.AddXUnit(outputHelper));
        _messageMonitor = new TrackingLoggingMonitor(_loggerFactory.CreateLogger<TrackingLoggingMonitor>());
        _middlewareMap = new MiddlewareMap();
    }

    public virtual async Task InitializeAsync()
    {
        Given();

        SystemUnderTest = CreateSystemUnderTestAsync();

        await When().ConfigureAwait(false);
    }

    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual void Given()
    {
        _typedMessage = new OrderAccepted();

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
            Uri = new Uri(ExpectedQueueUrl)
        };
        _serializationRegister.DeserializeMessage(Arg.Any<string>())
            .Returns(new MessageWithAttributes(_typedMessage, new MessageAttributes()));
    }

    private async Task When()
    {
        var queueReader = new SqsQueueReader(_queue);
        await SystemUnderTest.DispatchMessageAsync(queueReader.ToMessageContext(_sqsMessage), CancellationToken.None);
    }

    private MessageDispatcher CreateSystemUnderTestAsync()
    {
        var dispatcher = new MessageDispatcher(
            _serializationRegister,
            _messageMonitor,
            _middlewareMap,
            new MessageCompressionRegistry(new List<IMessageBodyCompression>()),
            _loggerFactory);

        return dispatcher;
    }

    public class AndHandlerMapDoesNotHaveMatchingHandler(ITestOutputHelper outputHelper) : WhenDispatchingMessage(outputHelper)
    {
        private const int ExpectedReceiveCount = 1;
        private readonly TimeSpan _expectedBackoffTimeSpan = TimeSpan.FromMinutes(4);

        protected override void Given()
        {
            base.Given();
            _sqsMessage.Attributes.Add(MessageSystemAttributeName.ApproximateReceiveCount, ExpectedReceiveCount.ToString(CultureInfo.InvariantCulture));
        }

        [Fact]
        public void ShouldDeserializeMessage()
        {
            _serializationRegister.Received(1).DeserializeMessage(Arg.Is<string>(x => x == _sqsMessage.Body));
        }

        [Fact]
        public void ShouldNotHandleMessage()
        {
            var testLogger = _loggerFactory.GetTestLoggerSink();
            testLogger.LogEntries.ShouldContain(le => le.OriginalFormat == "Failed to dispatch. Middleware for message of type '{MessageTypeName}' not found in middleware map.");
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
                .UseBackoff(MessageBackoffStrategy)
                .UseDefaults<SimpleMessage>(handler.GetType())
                .Build();

            _middlewareMap.Add<OrderAccepted>(_queue.QueueName, middleware);
        }

        [Fact]
        public void ShouldDeserializeMessage()
        {
            _serializationRegister.Received(1).DeserializeMessage(Arg.Is<string>(x => x == _sqsMessage.Body));
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
                .UseBackoff(MessageBackoffStrategy)
                .UseDefaults<SimpleMessage>(handler.GetType())
                .Build();

            _middlewareMap.Add<OrderAccepted>(_queue.QueueName, middleware);

            MessageBackoffStrategy.GetBackoffDuration(_typedMessage, 1, _expectedException).Returns(_expectedBackoffTimeSpan);
            _sqsMessage.Attributes.Add(MessageSystemAttributeName.ApproximateReceiveCount, ExpectedReceiveCount.ToString(CultureInfo.InvariantCulture));
        }

        [Fact]
        public void ShouldInvokeMessageBackoffStrategyWithNumberOfReceives()
        {
            MessageBackoffStrategy.Received(1).GetBackoffDuration(Arg.Is(_typedMessage), Arg.Is(ExpectedReceiveCount), Arg.Is(_expectedException));
        }

        [Fact]
        public void ShouldUpdateMessageVisibility()
        {
            var request = _queue.ChangeMessageVisbilityRequests.ShouldHaveSingleItem();
            request.QueueUrl.ShouldBe(ExpectedQueueUrl);
            request.ReceiptHandle.ShouldBe(_sqsMessage.ReceiptHandle);
            request.VisibilityTimeoutInSeconds.ShouldBe((int)_expectedBackoffTimeSpan.TotalSeconds);
        }
    }
}
