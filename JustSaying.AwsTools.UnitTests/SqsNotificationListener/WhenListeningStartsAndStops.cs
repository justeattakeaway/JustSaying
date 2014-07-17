using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
using JustBehave;
using NSubstitute;
using JustSaying.TestingFramework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenListeningStartsAndStops : BaseQueuePollingTest
    {
        private const string SubjectOfMessageAfterStop = "POST_STOP_MESSAGE";

        protected override void Given()
        {
            base.Given();
            Sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()).Returns(x => GenerateResponseMessage(SubjectOfMessageAfterStop, Guid.NewGuid()), x => new ReceiveMessageResponse { Messages = new List<Message>() });
        }

        protected override void When()
        {
            base.When();

            SystemUnderTest.StopListening();
            
            SystemUnderTest.Listen();
        }

        [Then]
        public void CorrectQueueIsPolled()
        {
            Patiently.VerifyExpectation(() => Sqs.Received().ReceiveMessage(Arg.Is<ReceiveMessageRequest>(x => x.QueueUrl == QueueUrl)));
        }

        [Then]
        public void TheMaxMessageAllowanceIsGrabbed()
        {
            Patiently.VerifyExpectation(() => Sqs.Received().ReceiveMessage(Arg.Is<ReceiveMessageRequest>(x => x.MaxNumberOfMessages == 10)));
        }

        [Then]
        public void MessageIsProcessed()
        {
            Patiently.VerifyExpectation(() => SerialisationRegister.Received().GetSerialiser(SubjectOfMessageAfterStop));
        }

        public override void PostAssertTeardown()
        {
            base.PostAssertTeardown();
            SystemUnderTest.StopListening();
        }
    }
}