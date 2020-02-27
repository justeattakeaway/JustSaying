using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.ConsumerBusTests
{
    public class WhenPassingAHandledAndUnhandledMessage : BaseConsumerBusTests
    {
        private ISqsQueue _queue;
        private string _messageBody = "Expected Message Body";
        private int _callCount;

        public WhenPassingAHandledAndUnhandledMessage(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            _queue = Substitute.For<ISqsQueue>();
            _queue.GetMessages(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(_ => new List<Message> { new TestMessage { Body = _messageBody } })
                .AndDoes(_ => Interlocked.Increment(ref _callCount));
            _queue.Uri.Returns(new Uri("http://foo.com"));

            Queues.Add(_queue);
            Handler.Handle(null)
                .ReturnsForAnyArgs(info => true)
                .AndDoes(x => Thread.Sleep(1)); // Ensure at least one ms wait on processing
        }

        [Fact]
        public void MessagesGetDeserializedByCorrectHandler()
        {
            SerializationRegister.Received()
                .DeserializeMessage(_messageBody);
        }

        [Fact]
        public void ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            Handler.Received().Handle(DeserializedMessage);
        }

        [Fact]
        public void MonitoringToldMessageHandlingTime()
        {
            Monitor.Received()
                .HandleTime(Arg.Is<TimeSpan>(x => x > TimeSpan.Zero));
        }

        [Fact]
        public void AllMessagesAreClearedFromQueue()
        {
            SerializationRegister.Received(_callCount).DeserializeMessage(Arg.Any<string>());

            _queue.Received().DeleteMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }

    /*
    Some more:
     * 1. Multiple handling of same message with different handlers
     * 2. etc
    */
}
