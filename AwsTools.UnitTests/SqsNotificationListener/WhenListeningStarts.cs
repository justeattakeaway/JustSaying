using Amazon.SQS.Model;
using JustEat.Testing;
using NSubstitute;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenListeningStarts : BaseQueuePollingTest
    {
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
    }
}