using Amazon.SQS.Model;
using JustBehave;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenMessageHandlingThrows : BaseQueuePollingTest
    {
        private bool _firstTime = true;

        protected override void Given()
        {
            base.Given();
            Handler.Handle(Arg.Any<GenericMessage>()).Returns(
                _ => ExceptionOnFirstCall());
        }

        private bool ExceptionOnFirstCall()
        {
            if (_firstTime)
            {
                _firstTime = false;
                throw new TestException("Thrown by test handler");
            }

            return false;
        }

        [Then]
        public void MessageHandlerWasCalled()
        {
            Handler.ReceivedWithAnyArgs().Handle(Arg.Any<GenericMessage>());
        }

        [Then]
        public void FailedMessageIsNotRemovedFromQueue()
        {
            Sqs.DidNotReceiveWithAnyArgs().DeleteMessageAsync(Arg.Any<DeleteMessageRequest>());
        }

        [Then]
        public void ExceptionIsLoggedToMonitor()
        {
            Monitor.ReceivedWithAnyArgs().HandleException(Arg.Any<string>());
        }
    }
}
