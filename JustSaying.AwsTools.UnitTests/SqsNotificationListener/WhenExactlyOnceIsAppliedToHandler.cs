using AwsTools.UnitTests.SqsNotificationListener;
using JustBehave;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using NSubstitute;
using TimeSpan = System.TimeSpan;

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
            Handler = new MyHandler();
        }

        [Then]
        public void ProcessingIsPassedToTheHandler()
        {
            Patiently.VerifyExpectation(() => (Handler as MyHandler).Received());
        }

        [Then]
        public void MessageIsLockedForTheRightId()
        {
            Patiently.VerifyExpectation(() => 
                MessageLock.Received().TryAquire(Arg.Is<string>(a => a.Contains(DeserialisedMessage.Id.ToString())), Arg.Any<TimeSpan>()));
        }

        [Then]
        public void MessageIsLockedWithCorrectTimeout()
        {
            Patiently.VerifyExpectation(() =>
                MessageLock.Received().TryAquire(Arg.Any<string>(), TimeSpan.FromSeconds(_expectedtimeout)));
        }
    }

    [ExactlyOnce(TimeOut = 5)]
    public class MyHandler : IHandler<GenericMessage>
    {
        private bool _received;
        public bool Handle(GenericMessage message)
        {
            _received = true;
            return true;
        }

        public bool Received()
        {
            return _received;
        }

    }
}