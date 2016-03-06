using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenExactlyOnceIsAppliedToHandler : BaseQueuePollingTest
    {
        private int _expectedtimeout;
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        protected override void Given()
        {
            var messageLockResponse = new MessageLockResponse
                {
                    DoIHaveExclusiveLock = true
                };

            base.Given();
            _expectedtimeout = 5;
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
            await Patiently.WaitWithTimeoutAsync(_tcs.Task);
        }

        [Test]
        public void ProcessingIsPassedToTheHandler()
        {
            ((ExactlyOnceSignallingHandler)Handler).HandlerWasCalled();
        }

        [Test]
        public void MessageIsLocked()
        {
            MessageLock.Received().TryAquireLock(
                Arg.Is<string>(a => a.Contains(DeserialisedMessage.Id.ToString())),
                TimeSpan.FromSeconds(_expectedtimeout));
        }
    }
}