using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenThereAreNoMessagesToProcess : BehaviourTest<JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener>
    {
        private readonly AmazonSQS _sqs = Substitute.For<AmazonSQS>();
        private int _callCount;

        protected override JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener CreateSystemUnderTest()
        {
            return new JustEat.Simples.NotificationStack.AwsTools.SqsNotificationListener(new SqsQueueByUrl("", _sqs), null, new NullMessageFootprintStore());
        }

        protected override void Given()
        {
            _sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()).Returns(x => GenerateEmptyMessage());
            _sqs.When(x => x.ReceiveMessage(Arg.Any<ReceiveMessageRequest>())).Do(x => _callCount++);
        }

        protected override void When()
        {
            SystemUnderTest.Listen();
            Thread.Sleep(20);
        }

        [Then]
        public void ListenLoopDoesNotDie()
        {
            Assert.GreaterOrEqual(_callCount, 3);
        }

        private ReceiveMessageResponse GenerateEmptyMessage()
        {
            return new ReceiveMessageResponse { ReceiveMessageResult = new ReceiveMessageResult() };
        }
    }
}