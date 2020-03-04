using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.ConsumerBusTests.Support;
using NSubstitute;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.ConsumerBusTests
{
    public class WhenExactlyOnceIsAppliedWithoutSpecificTimeout : BaseConsumerBusTests
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
            _queue = CreateSuccessfulTestQueue(new TestMessage());

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
            HandlerMap.Add(() => Handler);

            var cts = new CancellationTokenSource();
            await SystemUnderTest.Start(cts.Token);

            // wait until it's done
            await TaskHelpers.WaitWithTimeoutAsync(_tcs.Task);
            cts.Cancel();
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
