using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriberBusTests
{
    public class WhenListeningStartsAndStops : BaseSubscriptionBusTests
    {
        private ISqsQueue _queue;
        private const string _messageContentsRunning = @"Message Contents Running";
        private const string _messageContentsAfterStop = @"Message Contents After Stop";

        private int _expectedMaxMessageCount;
        private bool _running = false;

        public WhenListeningStartsAndStops(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            // we expect to get max 10 messages per batch
            _expectedMaxMessageCount = MessageConstants.MaxAmazonMessageCap;

            Logger.LogInformation("Expected max message count is {MaxMessageCount}", _expectedMaxMessageCount);

            var response1 = new List<Message> { new Message { Body = _messageContentsRunning } };
            var response2 = new List<Message> { new Message { Body = _messageContentsAfterStop } };

            _queue = CreateSuccessfulTestQueue(() => _running ? response1 : response2);

            Queues.Add(_queue);
        }

        protected override async Task WhenAsync()
        {
            _running = true;
            var cts = new CancellationTokenSource();

            var completion = SystemUnderTest.Run(cts.Token);

            cts.CancelAfter(TimeoutPeriod.Subtract(TimeSpan.FromMilliseconds(500)));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);
            _running = false;
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

            SerializationRegister.DidNotReceive()
                .DeserializeMessage(_messageContentsAfterStop);
        }
    }
}
