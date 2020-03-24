using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriberBusTests
{
    public class WhenPassingAHandledAndUnhandledMessage : BaseSubscriptionBusTests
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
            _queue = CreateSuccessfulTestQueue(() =>
            {
                Interlocked.Increment(ref _callCount);
                return new List<Message> { new TestMessage { Body = _messageBody } };
            });

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
    }

    /*
    Some more:
     * 1. Multiple handling of same message with different handlers
     * 2. etc
    */
}
