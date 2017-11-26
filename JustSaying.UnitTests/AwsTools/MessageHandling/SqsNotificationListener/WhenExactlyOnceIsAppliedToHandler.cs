using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener.Support;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenExactlyOnceIsAppliedToHandler : BaseQueuePollingTest
    {
        private int _expectedtimeout;
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
        private ExplicitExactlyOnceSignallingHandler _handler;

        protected override void Given()
        {
            base.Given();
            _expectedtimeout = 5;

            var messageLockResponse = new MessageLockResponse
                {
                    DoIHaveExclusiveLock = true
                };

            MessageLock = Substitute.For<IMessageLock>();
            MessageLock.TryAquireLock(Arg.Any<string>(), Arg.Any<TimeSpan>())
                .Returns(messageLockResponse);

            _handler = new ExplicitExactlyOnceSignallingHandler(_tcs);
            Handler = _handler;
        }

        protected override async Task When()
        {
            SystemUnderTest.AddMessageHandler(() => Handler);
            SystemUnderTest.Listen();

            // wait until it's done
            await Tasks.WaitWithTimeoutAsync(_tcs.Task);
            SystemUnderTest.StopListening();
            await Task.Yield();
        }

        [Fact]
        public void ProcessingIsPassedToTheHandler()
        {
            _handler.HandleWasCalled.ShouldBeTrue();
        }

        [Fact]
        public void MessageIsLocked()
        {
            var messageId = DeserialisedMessage.Id.ToString();

            MessageLock.Received().TryAquireLock(
                Arg.Is<string>(a => a.Contains(messageId)),
                TimeSpan.FromSeconds(_expectedtimeout));
        }
    }
}
