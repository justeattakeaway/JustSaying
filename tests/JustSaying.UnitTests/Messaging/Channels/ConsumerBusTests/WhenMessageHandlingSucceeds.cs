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
    public class WhenMessageHandlingSucceeds : BaseConsumerBusTests
    {
        private ISqsQueue _queue;
        private string _messageBody = "Expected Message Body";
        private int _callCount = 0;

        public WhenMessageHandlingSucceeds(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            _queue = CreateSuccessfulTestQueue(() =>
            {
                return new List<Message> { new TestMessage { Body = _messageBody } };
            });

            Queues.Add(_queue);
            Handler.Handle(null)
                .ReturnsForAnyArgs(true).AndDoes(ci => Interlocked.Increment(ref _callCount));
        }

        [Fact]
        public void MessagesGetDeserializedByCorrectHandler()
        {
            SerializationRegister.Received().DeserializeMessage(Arg.Is<string>(s => s ==_messageBody));
        }

        [Fact]
        public void ProcessingIsPassedToTheHandlerForCorrectMessage()
        {
            Handler.Received().Handle(DeserializedMessage);
        }

        [Fact]
        public void AllMessagesAreClearedFromQueue()
        {
            _queue.Received(_callCount).DeleteMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
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
