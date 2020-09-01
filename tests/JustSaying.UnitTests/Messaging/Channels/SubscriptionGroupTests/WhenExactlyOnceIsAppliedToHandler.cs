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
    public class WhenExactlyOnceIsAppliedToHandler : BaseSubscriptionGroupTests
    {
        private ISqsQueue _queue;
        private readonly int _expectedTimeout = 5;
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        private ExplicitExactlyOnceSignallingHandler _handler;

        public WhenExactlyOnceIsAppliedToHandler(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        { }

        protected override void Given()
        {
            _queue = CreateSuccessfulTestQueue("TestQueue", new TestMessage());

            Queues.Add(_queue);

            var messageLockResponse = new MessageLockResponse
            {
                DoIHaveExclusiveLock = true
            };

            MessageLock = new FakeMessageLock();

            _handler = new ExplicitExactlyOnceSignallingHandler(_tcs);
            Handler = _handler;
        }

        protected override async Task WhenAsync()
        {
            HandlerMap.Add(_queue.QueueName, () => Handler);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            var completion = SystemUnderTest.RunAsync(cts.Token);

            // wait until it's done
            await TaskHelpers.WaitWithTimeoutAsync(_tcs.Task);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);
        }

        [Fact]
        public void ProcessingIsPassedToTheHandler()
        {
            _handler.HandleWasCalled.ShouldBeTrue();
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
