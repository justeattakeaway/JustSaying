using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class WhenMessageHandlingSucceeds : BaseSubscriptionGroupTests
    {
        private FakeAmazonSqs _sqsClient;
        private string _messageBody = "Expected Message Body";

        public WhenMessageHandlingSucceeds(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        { }

        protected override void Given()
        {
            var queue = CreateSuccessfulTestQueue(Guid.NewGuid().ToString(),
                () => new List<Message> { new TestMessage { Body = _messageBody } });
            _sqsClient = queue.FakeClient;

            Queues.Add(queue);
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
            Handler.ReceivedMessages.ShouldContain(SerializationRegister.DefaultDeserializedMessage());
        }

        [Fact]
        public void AllMessagesAreClearedFromQueue()
        {
            var numberOfMessagesHandled = Handler.ReceivedMessages.Count;
            _sqsClient.DeleteMessageRequests.Count.ShouldBe(numberOfMessagesHandled);
        }

        [Fact]
        public void ReceiveMessageTimeStatsSent()
        {
            var numberOfMessagesHandled = Handler.ReceivedMessages.Count;

            // The receive buffer might receive messages that aren't handled before shutdown
            Monitor.ReceiveMessageTimes.Count.ShouldBeGreaterThanOrEqualTo(numberOfMessagesHandled);
        }

        [Fact]
        public void ExceptionIsNotLoggedToMonitor()
        {
            Monitor.HandledExceptions.ShouldBeEmpty();
        }
    }
}
