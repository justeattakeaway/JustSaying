using System;
using Amazon;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Testing;

namespace AwsTools.IntegrationTests
{
    public abstract class WhenCreatingQueuesByName : BehaviourTest<SqsQueueByName>
    {
        protected string QueueUniqueKey;

        protected override void Given()
        { }

        protected override SqsQueueByName CreateSystemUnderTest()
        {
            QueueUniqueKey = "test" + DateTime.Now.Ticks;
            return new SqsQueueByName(QueueUniqueKey, AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.EUWest1));
        }
        public override void PostAssertTeardown()
        {
            SystemUnderTest.Delete();
            base.PostAssertTeardown();
        }
    }
}
