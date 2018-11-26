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
    public class WhenExactlyOnceIsAppliedWithoutSpecificTimeout : BaseQueuePollingTest
    {
        //private readonly int _maximumTimeout = int.MaxValue;
        private readonly int _maximumTimeout = (int)TimeSpan.MaxValue.TotalSeconds;
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
        private ExactlyOnceSignallingHandler _handler;

        protected override async Task Given()
        {
            await base.Given();

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

        protected override async Task When()
        {
            SystemUnderTest.AddMessageHandler(() => Handler);
            var cts = new CancellationTokenSource();
            SystemUnderTest.Listen(cts.Token);

            // wait until it's done
            await Tasks.WaitWithTimeoutAsync(_tcs.Task);
            cts.Cancel();
        }

        [Fact]
        public void MessageIsLocked()
        {
            var messageId = DeserializedMessage.Id.ToString();

            MessageLock.Received().TryAquireLockAsync(
                Arg.Is<string>(a => a.Contains(messageId)),
                TimeSpan.FromSeconds(_maximumTimeout));
        }

        [Fact]
        public void ProcessingIsPassedToTheHandler()
        {
            _handler.HandleWasCalled.ShouldBeTrue();
        }
    }
}
