using System;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.TestingFramework;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class WhenMessageHandlingThrows : BaseSubscriptionGroupTests
    {
        private bool _firstTime = true;
        private IAmazonSQS _sqsClient;

        public WhenMessageHandlingThrows(ITestOutputHelper testOutputHelper)
            : base (testOutputHelper)
        {
        }

        protected override void Given()
        {
            var queue = CreateSuccessfulTestQueue("TestQueue", new TestMessage());
            _sqsClient = queue.Client;

            Queues.Add(queue);
            Handler.Handle(Arg.Any<SimpleMessage>())
                .Returns(_ => ExceptionOnFirstCall());
        }

        private bool ExceptionOnFirstCall()
        {
            if (_firstTime)
            {
                _firstTime = false;
                throw new TestException("Thrown by test handler");
            }

            return false;
        }

        [Fact]
        public void MessageHandlerWasCalled()
        {
            Handler.ReceivedWithAnyArgs().Handle(Arg.Any<SimpleMessage>());
        }

        [Fact]
        public void FailedMessageIsNotRemovedFromQueue()
        {
            _sqsClient.DidNotReceiveWithAnyArgs().DeleteMessageAsync(Arg.Any<DeleteMessageRequest>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void ExceptionIsLoggedToMonitor()
        {
            Monitor.ReceivedWithAnyArgs().HandleException(Arg.Any<Type>());
        }
    }
}
