using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS.Model;
using JustSaying.AwsTools;
using JustSaying.Messaging.Monitoring;
using JustBehave;
using NSubstitute;
using JustSaying.TestingFramework;

namespace AwsTools.UnitTests.SqsNotificationListener
{
    public class WhenThereAreNoMessagesToProcess : BehaviourTest<JustSaying.AwsTools.SqsNotificationListener>
    {
        private readonly ISqsClient _sqs = Substitute.For<ISqsClient>();
        private int _callCount;

        protected override JustSaying.AwsTools.SqsNotificationListener CreateSystemUnderTest()
        {
            return new JustSaying.AwsTools.SqsNotificationListener(new SqsQueueByUrl("", _sqs), null, Substitute.For<IMessageMonitor>());
        }

        protected override void Given()
        {
            _sqs.ReceiveMessage(Arg.Any<ReceiveMessageRequest>())
                .Returns(x => GenerateEmptyMessage());
            _sqs.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>())
                .Returns(x => Task.FromResult(GenerateEmptyMessage()));

            _sqs.When(x => x.ReceiveMessage(Arg.Any<ReceiveMessageRequest>()))
                .Do(x => _callCount++);
            _sqs.When(x => x.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>()))
                .Do(x => _callCount++);

            _sqs.Region.Returns(RegionEndpoint.EUWest1);
        }

        protected override void When()
        {
            SystemUnderTest.Listen();
        }

        [Then]
        public async Task ListenLoopDoesNotDie()
        {
            await Patiently.AssertThatAsync(() => _callCount > 3);
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