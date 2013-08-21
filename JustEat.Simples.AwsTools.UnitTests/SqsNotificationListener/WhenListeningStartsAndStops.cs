using System;
using System.Threading;
using Amazon.SQS.Model;
using JustEat.Testing;
using NSubstitute;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenListeningStartsAndStops : BaseQueuePollingTest
    {
        private const string SubjectOfMessageAfterStop = "POST_STOP_MESSAGE";

        protected override void When()
        {
            base.When();

            SystemUnderTest.StopListening();
            Sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()).Returns(x => GenerateResponseMessage(SubjectOfMessageAfterStop, Guid.NewGuid()), x => new ReceiveMessageResponse{ReceiveMessageResult = new ReceiveMessageResult()});
            SystemUnderTest.Listen();
            Thread.Sleep(20);
            SystemUnderTest.StopListening();
        }

        [Then]
        public void CorrectQueueIsPolled()
        {
            Sqs.Received().ReceiveMessage(Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == QueueUrl));
        }

        [Then]
        public void TheMaxMessageAllowanceIsGrabbed()
        {
            Sqs.Received().ReceiveMessage(Arg.Is<ReceiveMessageRequest>(x => x.MaxNumberOfMessages == 10));
        }

        [Then]
        public void MessageIsProcessed()
        {
            SerialisationRegister.Received().GetSerialiser(SubjectOfMessageAfterStop);
        }
    }
}