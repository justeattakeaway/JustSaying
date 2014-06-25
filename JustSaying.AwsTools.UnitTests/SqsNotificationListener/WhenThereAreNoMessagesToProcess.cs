using System.Collections.Generic;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools;
using JustSaying.Messaging.Monitoring;
using JustEat.Testing;
using NSubstitute;
using JustSaying.TestingFramework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenThereAreNoMessagesToProcess : BehaviourTest<JustSaying.AwsTools.SqsNotificationListener>
    {
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private int _callCount;

        protected override JustSaying.AwsTools.SqsNotificationListener CreateSystemUnderTest()
        {
            return new JustSaying.AwsTools.SqsNotificationListener(new SqsQueueByUrl("", _sqs), null, Substitute.For<IMessageMonitor>());
        }

        protected override void Given()
        {
            _sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()).Returns(x => GenerateEmptyMessage());
            _sqs.When(x => x.ReceiveMessage(Arg.Any<ReceiveMessageRequest>())).Do(x => _callCount++);
        }

        protected override void When()
        {
            SystemUnderTest.Listen();
        }

        [Then]
        public void ListenLoopDoesNotDie()
        {
            Patiently.AssertThat(() => _callCount > 3);
        }

        public override void PostAssertTeardown()
        {
            base.PostAssertTeardown();
            SystemUnderTest.StopListening();
        }

        private ReceiveMessageResponse GenerateEmptyMessage()
        {
            return new ReceiveMessageResponse { Messages = new List<Message>() };
        }
    }
}