using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriberBusTests.Support;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriberBusTests
{
    public class WhenExactlyOnceIsAppliedToHandler : BaseSubscriptionBusTests
    {
        private ISqsQueue _queue;
        private int _expectedTimeout = 5;
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        private ExplicitExactlyOnceSignallingHandler _handler;

        public WhenExactlyOnceIsAppliedToHandler(ITestOutputHelper testOutputHelper)
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

            _handler = new ExplicitExactlyOnceSignallingHandler(_tcs);
            Handler = _handler;
        }

        protected override async Task WhenAsync()
        {
            HandlerMap.Add(_queue.QueueName, () => Handler);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            var completion = SystemUnderTest.Run(cts.Token);

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
        public async Task MessageIsLocked()
        {
            var messageId = DeserializedMessage.Id.ToString();

            await MessageLock.Received().TryAquireLockAsync(
                Arg.Is<string>(a => a.Contains(messageId, StringComparison.OrdinalIgnoreCase)),
                TimeSpan.FromSeconds(_expectedTimeout));
        }
    }
}
