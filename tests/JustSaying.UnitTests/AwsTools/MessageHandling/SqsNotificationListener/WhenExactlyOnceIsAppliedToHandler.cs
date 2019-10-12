using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener.Support;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.SqsNotificationListener
{
    public class WhenExactlyOnceIsAppliedToHandler : BaseQueuePollingTest
    {
        private int _expectedTimeout;
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
        private ExplicitExactlyOnceSignallingHandler _handler;

        protected override void Given()
        {
            base.Given();
            _expectedTimeout = 5;

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
            SystemUnderTest.AddMessageHandler(() => Handler);
            var cts = new CancellationTokenSource();
            SystemUnderTest.Listen(cts.Token);

            // wait until it's done
            await TaskHelpers.WaitWithTimeoutAsync(_tcs.Task);
            cts.Cancel();
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
