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
    public class WhenMessageHandlingFails : BaseSubscriptionGroupTests
    {
        private IAmazonSQS _sqsClient;

        public WhenMessageHandlingFails(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            var queue = CreateSuccessfulTestQueue("TestQueue", new TestMessage());
            _sqsClient = queue.Client;

            Queues.Add(queue);
            Handler.Handle(Arg.Any<SimpleMessage>()).ReturnsForAnyArgs(false);
        }

        [Fact]
        public void MessageHandlerWasCalled()
        {
            Handler.ReceivedWithAnyArgs().Handle(Arg.Any<SimpleMessage>());
        }

        [Fact]
        public void FailedMessageIsNotRemovedFromQueue()
        {
            // The un-handled one is however.
            _sqsClient.DidNotReceiveWithAnyArgs().DeleteMessageAsync(Arg.Any<DeleteMessageRequest>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void ExceptionIsNotLoggedToMonitor()
        {
            Monitor.DidNotReceiveWithAnyArgs().HandleException(Arg.Any<Type>());
        }
    }
}
