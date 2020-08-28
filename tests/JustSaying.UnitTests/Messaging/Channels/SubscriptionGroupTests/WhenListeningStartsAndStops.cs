using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class WhenListeningStartsAndStops : BaseSubscriptionGroupTests
    {
        private ISqsQueue _queue;
#pragma warning disable IDE1006
        private const string _messageContentsRunning = @"Message Contents Running";
        private const string _messageContentsAfterStop = @"Message Contents After Stop";
#pragma warning restore IDE1006

        private int _expectedMaxMessageCount;
        private bool _running;
        private IAmazonSQS _client;

        public WhenListeningStartsAndStops(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            // we expect to get max 10 messages per batch
            _expectedMaxMessageCount = MessageDefaults.MaxAmazonMessageCap;

            Logger.LogInformation("Expected max message count is {MaxMessageCount}", _expectedMaxMessageCount);

            var response1 = new List<Message> { new Message { Body = _messageContentsRunning } };
            var response2 = new List<Message> { new Message { Body = _messageContentsAfterStop } };

            _queue = CreateSuccessfulTestQueue("TestQueue", () => _running ? response1 : response2);
            _client = _queue.Client;

            Queues.Add(_queue);
        }

        protected override async Task WhenAsync()
        {
            foreach (var queue in Queues)
            {
                HandlerMap.Add(queue.QueueName, typeof(SimpleMessage), msg => Task.FromResult(true));
            }

            _running = true;
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            var completion = SystemUnderTest.RunAsync(cts.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);
            _running = false;
        }

        [Fact]
        public async Task MessagesAreReceived()
        {
            await _client.Received()
                .ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task TheMaxMessageAllowanceIsGrabbed()
        {
            await _client.Received()
                .ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(request => request.MaxNumberOfMessages == _expectedMaxMessageCount), Arg.Any<CancellationToken>());
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
