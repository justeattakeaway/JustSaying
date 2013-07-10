using System.Threading;
using Amazon.SQS.Model;
using JustEat.Testing;
using NSubstitute;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenListeningStops : BaseQueuePollingTest
    {
        private const string SubjectOfMessageAfterStop = "POST_STOP_MESSAGE";

        protected override void When()
        {
            base.When();

            SystemUnderTest.StopListening();
            Sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()).Returns(x => GenerateResponseMessage(SubjectOfMessageAfterStop), x => new ReceiveMessageResponse());
            Thread.Sleep(30);
        }

        [Then]
        public void MessagesAfterStopAreNotProcessed()
        {
            _serialisationRegister.DidNotReceive().GetSerialiser(SubjectOfMessageAfterStop);
        }
    }
}