using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.ConsumerBusTests
{
    public class WhenListeningStartsAndStops : BaseConsumerBusTests
    {
        private ISqsQueue _queue;

        public WhenListeningStartsAndStops(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }
        private const string _messageContentsRunning = @"Message Contents Running";
        private const string _messageContentsAfterStop = @"Message Contents After Stop";

        private int _expectedMaxMessageCount;
        private bool _running = false;

        protected override void Given()
        {
            // we expect to get max 10 messages per batch
            // except on single-core machines when we top out at ParallelHandlerExecutionPerCore=8
            _expectedMaxMessageCount = Math.Min(MessageConstants.MaxAmazonMessageCap,
                Environment.ProcessorCount * MessageConstants.ParallelHandlerExecutionPerCore);

            var response1 = new List<Message> { new Message { Body = _messageContentsRunning } };
            var response2 = new List<Message> { new Message { Body = _messageContentsAfterStop } };

            _queue = CreateSuccessfulTestQueue(() => _running ? response1 : response2);

            Queues.Add(_queue);
        }

        protected override async Task WhenAsync()
        {
            _running = true;
            var cts = new CancellationTokenSource();

            _ = SystemUnderTest.Run(cts.Token);

            // todo: should this be needed/should Start only complete when everything is running?
            await Task.Delay(TimeSpan.FromMilliseconds(100));

            _running = false;
            cts.Cancel();
        }

        [Fact]
        public void MessagesAreReceived()
        {
            _queue.Received()
                .GetMessagesAsync(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void TheMaxMessageAllowanceIsGrabbed()
        {
            _queue.Received()
                .GetMessagesAsync(Arg.Is<int>(count => count == _expectedMaxMessageCount), Arg.Any<List<string>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void MessageIsProcessed()
        {
            SerializationRegister.Received()
                .DeserializeMessage(_messageContentsRunning);
            SerializationRegister.Received()
                .DeserializeMessage(_messageContentsAfterStop);
        }
    }
}

