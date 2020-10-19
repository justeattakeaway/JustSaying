using System.Linq;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public class WhenMessageHandlingThrows : BaseSubscriptionGroupTests
    {
        private bool _firstTime = true;
        private FakeAmazonSqs _sqsClient;

        public WhenMessageHandlingThrows(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        { }

        protected override void Given()
        {
            var queue = CreateSuccessfulTestQueue("TestQueue", new TestMessage());
            _sqsClient = queue.FakeClient;

            Queues.Add(queue);

            Middleware.Handler.OnHandle = (msg) =>
            {
                if (!_firstTime) return;

                _firstTime = false;
                throw new TestException("Thrown by test handler");
            };
        }

        [Fact]
        public void MessageHandlerWasCalled()
        {
            Middleware.Handler.ReceivedMessages.Any(msg => msg.GetType() == typeof(SimpleMessage)).ShouldBeTrue();
        }

        [Fact]
        public void FailedMessageIsNotRemovedFromQueue()
        {
            var numberHandled = Middleware.Handler.ReceivedMessages.Count;
            _sqsClient.DeleteMessageRequests.Count.ShouldBe(numberHandled - 1);
        }

        [Fact]
        public void ExceptionIsLoggedToMonitor()
        {
            Monitor.HandledExceptions.ShouldNotBeEmpty();
        }
    }
}
