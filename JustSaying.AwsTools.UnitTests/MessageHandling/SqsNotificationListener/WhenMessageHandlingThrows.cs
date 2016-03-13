using System;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.AwsTools.UnitTests.MessageHandling.SqsNotificationListener
{
    public class WhenMessageHandlingThrows : BaseQueuePollingTest
    {
        protected override void Given()
        {
            base.Given();
            Handler.Handle(Arg.Any<GenericMessage>()).Returns(
                x => { throw new Exception("Thrown by test handler"); },
                x => false );
        }

        [Then]
        public void MessageHandlerWasCalled()
        {
            Handler.ReceivedWithAnyArgs().Handle(Arg.Any<GenericMessage>());
        }

        [Then]
        public void FailedMessageIsNotRemovedFromQueue()
        {
            Sqs.DidNotReceiveWithAnyArgs().DeleteMessage(Arg.Any<DeleteMessageRequest>());
        }

        [Then]
        public void ExceptionIsLoggedToMonitor()
        {
            Monitor.ReceivedWithAnyArgs().HandleException(Arg.Any<string>());
        }
    }
}