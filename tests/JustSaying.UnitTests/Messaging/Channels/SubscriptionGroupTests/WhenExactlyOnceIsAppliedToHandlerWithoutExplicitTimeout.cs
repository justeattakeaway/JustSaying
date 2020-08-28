using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests.Support;
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
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
        private ExactlyOnceSignallingHandler _handler;

        public WhenExactlyOnceIsAppliedWithoutSpecificTimeout(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected override void Given()
        {
            _queue = CreateSuccessfulTestQueue("TestQueue", new TestMessage());

            Queues.Add(_queue);

            var messageLockResponse = new MessageLockResponse
            {
                DoIHaveExclusiveLock = true
            };

            MessageLock = Substitute.For<IMessageLockAsync>();
            MessageLock.TryAquireLockAsync(Arg.Any<string>(), Arg.Any<TimeSpan>())
                .Returns(messageLockResponse);

            _handler = new ExactlyOnceSignallingHandler(_tcs);
            Handler = _handler;
        }

        protected override async Task WhenAsync()
        {
            HandlerMap.Add(_queue.QueueName, () => Handler);

            var cts = new CancellationTokenSource();

            var completion = SystemUnderTest.RunAsync(cts.Token);

            cts.CancelAfter(TimeoutPeriod);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);

            // wait until it's done
            await TaskHelpers.WaitWithTimeoutAsync(_tcs.Task);
        }

        [Fact]
        public void MessageIsLocked()
        {
            var messageId = DeserializedMessage.Id.ToString();

            MessageLock.Received().TryAquireLockAsync(
                Arg.Is<string>(a => a.Contains(messageId, StringComparison.OrdinalIgnoreCase)),
                TimeSpan.FromSeconds(_maximumTimeout));
        }

        [Fact]
        public void ProcessingIsPassedToTheHandler()
        {
            _handler.HandleWasCalled.ShouldBeTrue();
        }
    }
}
