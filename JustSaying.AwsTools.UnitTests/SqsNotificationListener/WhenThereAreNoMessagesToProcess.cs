using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
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
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private int _callCount;

        protected override JustSaying.AwsTools.SqsNotificationListener CreateSystemUnderTest()
        {
            return new JustSaying.AwsTools.SqsNotificationListener(new SqsQueueByUrl(RegionEndpoint.EUWest1, "", _sqs), null, Substitute.For<IMessageMonitor>());
        }

        protected override void Given()
        {
            _sqs.ReceiveMessageAsync(
                    Arg.Any<ReceiveMessageRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(x => Task.FromResult(GenerateEmptyMessage()));
            
            _sqs.When(x => 
                    x.ReceiveMessageAsync(
                        Arg.Any<ReceiveMessageRequest>(),
                        Arg.Any<CancellationToken>()))
                .Do(x => _callCount++);
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