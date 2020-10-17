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
    public class WhenExactlyOnceIsAppliedWithoutSpecificTimeout : BaseSubscriptionGroupTests
    {
        private ISqsQueue _queue;
        private readonly int _maximumTimeout = (int)TimeSpan.MaxValue.TotalSeconds;
        private ExactlyOnceSignallingHandler _handler;

        public WhenExactlyOnceIsAppliedWithoutSpecificTimeout(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            _queue = CreateSuccessfulTestQueue(Guid.NewGuid().ToString(), new TestMessage());

            Queues.Add(_queue);

            MessageLock = new FakeMessageLock();

            _handler = new ExactlyOnceSignallingHandler();
            Handler = _handler;
        }

        protected override async Task WhenAsync()
        {
            MiddlewareMap.Add(_queue.QueueName, () => Handler);

            var cts = new CancellationTokenSource();

            var completion = SystemUnderTest.RunAsync(cts.Token);

            await Patiently.AssertThatAsync(OutputHelper,
                () => Handler.ReceivedMessages.Any());

            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);

        }

        [Fact]
        public void MessageIsLocked()
        {
            var messageId = SerializationRegister.DefaultDeserializedMessage().Id.ToString();

            var tempLockRequests = MessageLock.MessageLockRequests.Where(lr => !lr.isPermanent);
            tempLockRequests.Count().ShouldBeGreaterThan(0);
            tempLockRequests.ShouldAllBe(pair =>
                pair.key.Contains(messageId, StringComparison.OrdinalIgnoreCase) &&
                pair.howLong == TimeSpan.FromSeconds(_maximumTimeout));
        }
    }
}
