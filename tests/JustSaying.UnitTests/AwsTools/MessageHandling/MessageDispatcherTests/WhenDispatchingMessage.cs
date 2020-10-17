using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Xunit.Abstractions;
using Message = JustSaying.Models.Message;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.MessageDispatcherTests
{
    public class DummySqsQueue : SqsQueueBase
    {
        public DummySqsQueue(Uri uri, IAmazonSQS client, ILoggerFactory loggerFactory)
            : base(RegionEndpoint.EUWest1, client, loggerFactory)
        {
            Uri = uri;
            QueueName = "DummySqsQueue";
        }

        public override Task<bool> ExistsAsync() => Task.FromResult(true);
    }

    public class WhenDispatchingMessage : IAsyncLifetime
    {
        private const string ExpectedQueueUrl = "http://testurl.com/queue";

        private readonly IMessageSerializationRegister _serializationRegister = Substitute.For<IMessageSerializationRegister>();
        private readonly IMessageMonitor _messageMonitor = Substitute.For<IMessageMonitor>();
        private readonly MiddlewareMap _middlewareMap = new MiddlewareMap(Substitute.For<IMessageMonitor>(), NullLoggerFactory.Instance);
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMessageBackoffStrategy _messageBackoffStrategy = Substitute.For<IMessageBackoffStrategy>();
        private IAmazonSQS _amazonSqsClient = Substitute.For<IAmazonSQS>();

        private DummySqsQueue _queue;
        private SQSMessage _sqsMessage;
        private Message _typedMessage = new SimpleMessage();

        internal MessageDispatcher SystemUnderTest { get; private set; }

        public WhenDispatchingMessage(ITestOutputHelper outputHelper)
        {
            _loggerFactory = outputHelper.ToLoggerFactory();
        }

        public virtual async Task InitializeAsync()
        {
            Given();

            SystemUnderTest = CreateSystemUnderTestAsync();

            await When().ConfigureAwait(false);
        }

        public virtual Task DisposeAsync()
        {
            if (_amazonSqsClient != null)
            {
                _amazonSqsClient.Dispose();
                _amazonSqsClient = null;
            }

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

            _queue = new DummySqsQueue(new Uri(ExpectedQueueUrl),
                _amazonSqsClient, _loggerFactory);
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
                _loggerFactory,
                _messageBackoffStrategy,
                new MessageContextAccessor());

            return dispatcher;
        }

        public class AndHandlerMapDoesNotHaveMatchingHandler : WhenDispatchingMessage
        {
            private const int ExpectedReceiveCount = 1;
            private readonly TimeSpan _expectedBackoffTimeSpan = TimeSpan.FromMinutes(4);

            protected override void Given()
            {
                base.Given();
                _messageBackoffStrategy.GetBackoffDuration(_typedMessage, 1, null).Returns(_expectedBackoffTimeSpan);
                _sqsMessage.Attributes.Add(MessageSystemAttributeName.ApproximateReceiveCount, ExpectedReceiveCount.ToString(CultureInfo.InvariantCulture));
            }

            [Fact]
            public void ShouldDeserializeMessage()
            {
                _serializationRegister.Received(1).DeserializeMessage(Arg.Is<string>(x => x == _sqsMessage.Body));
            }

            [Fact]
            public void ShouldUpdateMessageVisibility()
            {
                _amazonSqsClient.Received(1).ChangeMessageVisibilityAsync(Arg.Is<ChangeMessageVisibilityRequest>(x => x.QueueUrl == ExpectedQueueUrl && x.ReceiptHandle == _sqsMessage.ReceiptHandle && x.VisibilityTimeout == (int)_expectedBackoffTimeSpan.TotalSeconds));
            }

            public AndHandlerMapDoesNotHaveMatchingHandler(ITestOutputHelper outputHelper) : base(outputHelper)
            { }
        }

        public class AndMessageProcessingSucceeds : WhenDispatchingMessage
        {
            protected override void Given()
            {
                base.Given();
                _middlewareMap.Add(_queue.QueueName, typeof(OrderAccepted), m => Task.FromResult(true));
            }

            [Fact]
            public void ShouldDeserializeMessage()
            {
                _serializationRegister.Received(1).DeserializeMessage(Arg.Is<string>(x => x == _sqsMessage.Body));
            }

            [Fact]
            public void ShouldDeleteMessageIfHandledSuccessfully()
            {
                _amazonSqsClient.Received(1).DeleteMessageAsync(Arg.Is<DeleteMessageRequest>(x => x.QueueUrl == ExpectedQueueUrl && x.ReceiptHandle == _sqsMessage.ReceiptHandle));
            }

            public AndMessageProcessingSucceeds(ITestOutputHelper outputHelper) : base(outputHelper)
            { }
        }

        public class AndMessageProcessingFails : WhenDispatchingMessage
        {
            private const int ExpectedReceiveCount = 1;
            private readonly TimeSpan _expectedBackoffTimeSpan = TimeSpan.FromMinutes(4);
            private readonly Exception _expectedException = new Exception("Something failed when processing");

            protected override void Given()
            {
                base.Given();
                _messageBackoffStrategy.GetBackoffDuration(_typedMessage, 1, _expectedException).Returns(_expectedBackoffTimeSpan);
                _middlewareMap.Add(_queue.QueueName, typeof(OrderAccepted), m => throw _expectedException);
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
                _amazonSqsClient.Received(1).ChangeMessageVisibilityAsync(Arg.Is<ChangeMessageVisibilityRequest>(x => x.QueueUrl == ExpectedQueueUrl && x.ReceiptHandle == _sqsMessage.ReceiptHandle && x.VisibilityTimeout == (int)_expectedBackoffTimeSpan.TotalSeconds));
            }

            public AndMessageProcessingFails(ITestOutputHelper outputHelper) : base(outputHelper)
            { }
        }
    }
}
