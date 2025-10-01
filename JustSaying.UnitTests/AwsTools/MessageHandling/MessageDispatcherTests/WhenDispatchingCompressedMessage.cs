using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Message = JustSaying.Models.Message;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.MessageDispatcherTests
{
    public class WhenDispatchingCompressedMessage : XAsyncBehaviourTest<MessageDispatcher>
    {
        private const string ExpectedQueueUrl = "http://queueurl";

        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly IMessageMonitor _messageMonitor = Substitute.For<IMessageMonitor>();
        private readonly Action<Exception, SQSMessage> _onError = Substitute.For<Action<Exception, SQSMessage>>();
        private readonly HandlerMap _handlerMap = new HandlerMap();
        private readonly ILoggerFactory _loggerFactory = Substitute.For<ILoggerFactory>();
        private readonly ILogger _logger = Substitute.For<ILogger>();
        private readonly IMessageBackoffStrategy _messageBackoffStrategy = Substitute.For<IMessageBackoffStrategy>();
        private readonly IAmazonSQS _amazonSqsClient = Substitute.For<IAmazonSQS>();
        private readonly IMessageBodyCompression _compression = new GzipMessageBodyCompression();

        private DummySqsQueue _queue;
        private SQSMessage _sqsMessage;
        private Message _typedMessage;
        private string _plainMessageBody;

        protected override void Given()
        {
            _typedMessage = new OrderAccepted();
            _plainMessageBody = JsonConvert.SerializeObject(_typedMessage);

            _sqsMessage = new SQSMessage
            {
                Body = _compression.Compress(_plainMessageBody),
                ReceiptHandle = "i_am_receipt_handle"
            };

            _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(_logger);
            _queue = new DummySqsQueue(ExpectedQueueUrl, _amazonSqsClient);
            _serialisationRegister.DeserializeMessage(Arg.Any<string>()).Returns(_typedMessage);
        }

        protected override async Task When() => await SystemUnderTest.DispatchMessage(_sqsMessage);

        protected override MessageDispatcher CreateSystemUnderTest()
        {
            return new MessageDispatcher(_queue, _serialisationRegister, _compression, _messageMonitor, _onError, _handlerMap, _loggerFactory, _messageBackoffStrategy);
        }

        public class AndMessageProcessingSucceeds : WhenDispatchingCompressedMessage
        {
            protected override void Given()
            {
                base.Given();
                _handlerMap.Add(typeof(OrderAccepted), m => Task.FromResult(true));
            }

            [Fact]
            public void ShouldDeserializeMessageWithDecompressedBody()
            {
                _serialisationRegister.Received(1).DeserializeMessage(Arg.Is<string>(x => x == _plainMessageBody));
            }

            [Fact]
            public void ShouldDeleteMessageIfHandledSuccessfully()
            {
                _amazonSqsClient.Received(1).DeleteMessageAsync(Arg.Is<DeleteMessageRequest>(x => x.QueueUrl == ExpectedQueueUrl && x.ReceiptHandle == _sqsMessage.ReceiptHandle));
            }
        }

        public class AndMessageProcessingFails : WhenDispatchingCompressedMessage
        {
            private const int ExpectedReceiveCount = 1;
            private readonly TimeSpan _expectedBackoffTimeSpan = TimeSpan.FromMinutes(4);
            private readonly Exception _expectedException = new Exception("Something failed when processing");

            protected override void Given()
            {
                base.Given();
                _messageBackoffStrategy.GetBackoffDuration(_typedMessage, 1, _expectedException).Returns(_expectedBackoffTimeSpan);
                _handlerMap.Add(typeof(OrderAccepted), m => throw _expectedException);
                _sqsMessage.Attributes.Add(MessageSystemAttributeName.ApproximateReceiveCount, ExpectedReceiveCount.ToString());
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
        }
    }
}
