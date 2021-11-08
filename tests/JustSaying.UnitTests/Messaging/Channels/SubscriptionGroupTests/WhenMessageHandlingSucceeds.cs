using Amazon.SQS.Model;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class WhenMessageHandlingSucceeds : BaseSubscriptionGroupTests
    {
        private string _messageBody = "Expected Message Body";
        private FakeSqsQueue _queue;

        public WhenMessageHandlingSucceeds(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        { }

        protected override void Given()
        {
            _queue = CreateSuccessfulTestQueue(Guid.NewGuid().ToString(),
                ct => Task.FromResult(new List<Message> { new TestMessage { Body = _messageBody } }.AsEnumerable()));

            Queues.Add(_queue);
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
            _queue.DeleteMessageRequests.Count.ShouldBe(Handler.ReceivedMessages.Count);
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
