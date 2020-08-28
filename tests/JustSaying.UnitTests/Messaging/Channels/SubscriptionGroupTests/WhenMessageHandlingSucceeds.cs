using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class WhenMessageHandlingSucceeds : BaseSubscriptionGroupTests
    {
        private IAmazonSQS _sqsClient;
        private string _messageBody = "Expected Message Body";
        private int _callCount;

        public WhenMessageHandlingSucceeds(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            var queue = CreateSuccessfulTestQueue("TestQueue", () =>
            {
                return new List<Message> { new TestMessage { Body = _messageBody } };
            });
            _sqsClient = queue.Client;

            Queues.Add(queue);
            Handler.Handle(null)
                .ReturnsForAnyArgs(true).AndDoes(ci => Interlocked.Increment(ref _callCount));
        }

        [Fact]
        public void MessagesGetDeserializedByCorrectHandler()
        {
            SerializationRegister.ReceivedDeserializationRequests.ShouldAllBe(
                msg => msg == _messageBody);
        }

        [Fact]
        public void ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            Handler.Received().Handle(SerializationRegister.DefaultDeserializedMessage());
        }

        [Fact]
        public void AllMessagesAreClearedFromQueue()
        {
            _sqsClient.Received(_callCount).DeleteMessageAsync(Arg.Any<DeleteMessageRequest>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void ReceiveMessageTimeStatsSent()
        {
            Monitor.Received().ReceiveMessageTime(Arg.Any<TimeSpan>(), Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public void ExceptionIsNotLoggedToMonitor()
        {
            Monitor.DidNotReceiveWithAnyArgs().HandleException(Arg.Any<Type>());
        }
    }
}
