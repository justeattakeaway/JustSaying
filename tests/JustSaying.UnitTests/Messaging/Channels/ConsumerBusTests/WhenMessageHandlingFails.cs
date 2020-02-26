using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.ConsumerBusTests
{
    public class WhenMessageHandlingFails : BaseConsumerBusTests
    {
        private ISqsQueue _queue;

        public WhenMessageHandlingFails(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            _queue = Substitute.For<ISqsQueue>();
            _queue.GetMessages(Arg.Any<int>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
                .Returns(_ => new List<Message> { new TestMessage() });
            _queue.Uri.Returns(new Uri("http://foo.com"));

            Queues.Add(_queue);
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
            _queue.DidNotReceiveWithAnyArgs().DeleteMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void ExceptionIsNotLoggedToMonitor()
        {
            Monitor.DidNotReceiveWithAnyArgs().HandleException(Arg.Any<Type>());
        }
    }
}
