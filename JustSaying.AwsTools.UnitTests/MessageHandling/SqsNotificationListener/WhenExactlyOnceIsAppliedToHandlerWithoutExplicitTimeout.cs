using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener.Support;
using JustSaying.Messaging.MessageHandling;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenExactlyOnceIsAppliedWithoutSpecificTimeout : BaseQueuePollingTest
    {
        private readonly int _maximumTimeout = (int)TimeSpan.MaxValue.TotalSeconds;
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        protected override void Given()
        {
            base.Given();

            var messageLockResponse = new MessageLockResponse
            {
                DoIHaveExclusiveLock = true
            };

            MessageLock = Substitute.For<IMessageLock>();
            MessageLock.TryAquireLock(Arg.Any<string>(), Arg.Any<TimeSpan>())
                .Returns(messageLockResponse);

            Handler = new ExactlyOnceSignallingHandler(_tcs);
        }

        protected override async Task When()
        {
            SystemUnderTest.AddMessageHandler(() => Handler);
            SystemUnderTest.Listen();

            // wait until it's done
            await Tasks.WaitWithTimeoutAsync(_tcs.Task);
        }

        [Test]
        public void MessageIsLocked()
        {
            MessageLock.Received().TryAquireLock(
                Arg.Is<string>(a => a.Contains(DeserialisedMessage.Id.ToString())),
                TimeSpan.FromSeconds(_maximumTimeout));
        }
    }
}