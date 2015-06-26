using System;
using AwsTools.UnitTests.SqsNotificationListener;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenExactlyOnceIsAppliedToHandler : BaseQueuePollingTest
    {
        private int _expectedtimeout;

        protected override void Given()
        {
            base.Given();
            _expectedtimeout = 5;
            MessageLock = Substitute.For<IMessageLock>();
            MessageLock.TryAquireLock(Arg.Any<string>(), Arg.Any<TimeSpan>()).Returns(new MessageLockResponse(){DoIHaveExclusiveLock = true});
            Handler = new MyHandler();
        }

        [Test]
        public void ProcessingIsPassedToTheHandler()
        {
            Patiently.VerifyExpectation(() => (Handler as MyHandler).HandlerWasCalled());
        }

        [Test]
        public void MessageIsLocked()
        {
            Patiently.VerifyExpectation(() =>
                MessageLock.Received().TryAquireLock(Arg.Is<string>(a => a.Contains(DeserialisedMessage.Id.ToString())), TimeSpan.FromSeconds(_expectedtimeout)));
        }
    }

    [ExactlyOnce(TimeOut = 5)]
    public class MyHandler : IHandler<GenericMessage>
    {
        private bool _handlerWasCalled;
        public bool Handle(GenericMessage message)
        {
            _handlerWasCalled = true;
            return true;
        }

        public bool HandlerWasCalled()
        {
            return _handlerWasCalled;
        }

    }
}