using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.ConsumerBusTests
{
    public class WhenMessageHandlingThrows : BaseSubscriptionBusTests
    {
        private bool _firstTime = true;
        private ISqsQueue _queue;

        public WhenMessageHandlingThrows(ITestOutputHelper testOutputHelper)
            : base (testOutputHelper)
        {
        }

        protected override void Given()
        {
            _queue = CreateSuccessfulTestQueue(new TestMessage());

            Queues.Add(_queue);
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
            _queue.DidNotReceiveWithAnyArgs().DeleteMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void ExceptionIsLoggedToMonitor()
        {
            Monitor.ReceivedWithAnyArgs().HandleException(Arg.Any<Type>());
        }
    }
}
