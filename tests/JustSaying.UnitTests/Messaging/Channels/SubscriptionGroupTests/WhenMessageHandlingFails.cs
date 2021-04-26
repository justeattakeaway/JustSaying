using System;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class WhenMessageHandlingFails : BaseSubscriptionGroupTests
    {
        private FakeAmazonSqs _sqsClient;

        public WhenMessageHandlingFails(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            var queue = CreateSuccessfulTestQueue(Guid.NewGuid().ToString(), new TestMessage());
            _sqsClient = queue.FakeClient;

            Queues.Add(queue);
            Handler.ShouldSucceed = false;
        }

        [Fact]
        public void MessageHandlerWasCalled()
        {
            Handler.ReceivedMessages.ShouldNotBeEmpty();
        }

        [Fact]
        public void FailedMessageIsNotRemovedFromQueue()
        {
            _sqsClient.DeleteMessageRequests.ShouldBeEmpty();
        }

        [Fact]
        public void ExceptionIsNotLoggedToMonitor()
        {
            Monitor.HandledExceptions.ShouldBeEmpty();
        }
    }
}
