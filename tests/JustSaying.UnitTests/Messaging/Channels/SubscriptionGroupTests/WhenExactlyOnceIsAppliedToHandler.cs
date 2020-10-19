using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests.Support;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    [ExactlyOnce(TimeOut = 5)]
    public class ExactlyOnceHandler : InspectableHandler<SimpleMessage>
    {

    }

    public class WhenExactlyOnceIsAppliedToHandler : BaseSubscriptionGroupTests
    {
        private ISqsQueue _queue;
        private readonly int _expectedTimeout = 5;

        public WhenExactlyOnceIsAppliedToHandler(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        { }

        protected override void Given()
        {

            _queue = CreateSuccessfulTestQueue("TestQueue",  new TestMessage());

            Queues.Add(_queue);

            MessageLock = new FakeMessageLock();
        }

        protected override async Task WhenAsync()
        {
            MiddlewareMap.Add<SimpleMessage>(_queue.QueueName, () => Middleware);

            using var cts = new CancellationTokenSource();

            var completion = SystemUnderTest.RunAsync(cts.Token);

            // wait until it's done
            await Patiently.AssertThatAsync(OutputHelper,
                () => Middleware.Handler.ReceivedMessages.Any());

            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);
        }

        [Fact]
        public void ProcessingIsPassedToTheHandler()
        {
            Middleware.Handler.ReceivedMessages.ShouldNotBeEmpty();
        }

        [Fact]
        public void MessageIsLocked()
        {
            var messageId = SerializationRegister.DefaultDeserializedMessage().Id.ToString();

            var tempLockRequests = MessageLock.MessageLockRequests.Where(lr => !lr.isPermanent);
            tempLockRequests.Count().ShouldBeGreaterThan(0);
            tempLockRequests.ShouldAllBe(pair =>
                pair.key.Contains(messageId, StringComparison.OrdinalIgnoreCase) &&
                pair.howLong == TimeSpan.FromSeconds(_expectedTimeout));
        }
    }
}
