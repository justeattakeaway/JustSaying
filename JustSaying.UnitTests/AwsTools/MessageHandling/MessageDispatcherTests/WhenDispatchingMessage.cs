using System;
using System.Globalization;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
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
    public class DummySqsQueue : SqsQueueBase
    {
        public DummySqsQueue(Uri uri, IAmazonSQS client) : base(RegionEndpoint.EUWest1, client)
        {
            Uri = uri;
        }

        public override Task<bool> ExistsAsync() => Task.FromResult(true);
    }
    
    public class WhenDispatchingMessage : XAsyncBehaviourTest<MessageDispatcher>
    {
        private const string ExpectedQueueUrl = "http://testurl.com/queue";
        
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly IMessageMonitor _messageMonitor = Substitute.For<IMessageMonitor>();
        private readonly Action<Exception, SQSMessage> _onError = Substitute.For<Action<Exception, SQSMessage>>();
        private readonly HandlerMap _handlerMap = new HandlerMap();
        private readonly ILoggerFactory _loggerFactory = Substitute.For<ILoggerFactory>();
        private readonly ILogger _logger = Substitute.For<ILogger>();
        private readonly IMessageBackoffStrategy _messageBackoffStrategy = Substitute.For<IMessageBackoffStrategy>();
        private readonly IAmazonSQS _amazonSqsClient = Substitute.For<IAmazonSQS>();
        
        private DummySqsQueue _queue;
        private SQSMessage _sqsMessage;
        private Message _typedMessage;
        
        protected override void Given()
        {
            _typedMessage = new OrderAccepted();

            _sqsMessage = new SQSMessage
            {
                Body = JsonConvert.SerializeObject(_typedMessage),
                ReceiptHandle = "i_am_receipt_handle"
            };

            _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(_logger);
            _queue = new DummySqsQueue(new Uri(ExpectedQueueUrl), _amazonSqsClient);
            _serialisationRegister.DeserializeMessage(Arg.Any<string>()).Returns(_typedMessage);
        }

        protected override async Task When() =>  await SystemUnderTest.DispatchMessage(_sqsMessage);

        protected override MessageDispatcher CreateSystemUnderTest()
        {
            return new MessageDispatcher(_queue, _serialisationRegister, _messageMonitor, _onError, _handlerMap, _loggerFactory, _messageBackoffStrategy);
        }

        public class AndMessageProcessingSucceeds : WhenDispatchingMessage
        {
            protected override void Given()
            {
                base.Given();
                _handlerMap.Add(typeof(OrderAccepted), m => Task.FromResult(true));
            }

            [Fact]
            public void ShouldDeserializeMessage()
            {
                _serialisationRegister.Received(1).DeserializeMessage(Arg.Is<string>(x => x == _sqsMessage.Body));
            }

            [Fact]
            public void ShouldDeleteMessageIfHandledSuccessfully()
            {
                _amazonSqsClient.Received(1).DeleteMessageAsync(Arg.Is<DeleteMessageRequest>(x => x.QueueUrl == ExpectedQueueUrl && x.ReceiptHandle == _sqsMessage.ReceiptHandle));
            }
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
                _handlerMap.Add(typeof(OrderAccepted), m => throw _expectedException);
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
                _amazonSqsClient.Received(1).ChangeMessageVisibilityAsync(Arg.Is<ChangeMessageVisibilityRequest>(x => x.QueueUrl == ExpectedQueueUrl && x.ReceiptHandle == _sqsMessage.ReceiptHandle && x.VisibilityTimeout == (int) _expectedBackoffTimeSpan.TotalSeconds));
            }
        }

        public class AndUpdatingMessageVisibilityErrors : WhenDispatchingMessage
        {
            protected override void Given()
            {
                base.Given();
                _messageBackoffStrategy.GetBackoffDuration(_typedMessage, Arg.Any<int>()).Returns(TimeSpan.FromMinutes(4));
                _amazonSqsClient.ChangeMessageVisibilityAsync(Arg.Any<ChangeMessageVisibilityRequest>()).Throws(new Exception("Something gone wrong"));

                _handlerMap.Add(typeof(OrderAccepted), m => Task.FromResult(false));
                _sqsMessage.Attributes.Add(MessageSystemAttributeName.ApproximateReceiveCount, "1");
            }

            [Fact]
            public void ShouldLogException()
            {
                _logger.ReceivedWithAnyArgs().LogError(0, null, "msg");
            }
        }
    }
}
