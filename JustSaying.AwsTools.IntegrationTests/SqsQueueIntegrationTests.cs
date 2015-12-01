using System;
using Amazon;
using JustSaying.AwsTools;
using JustBehave;

namespace JustSaying.AwsTools.IntegrationTests
{
    public abstract class WhenCreatingQueuesByName : BehaviourTest<SqsQueueByName>
    {
        protected string QueueUniqueKey;

        protected override void Given()
        { }

        protected override SqsQueueByName CreateSystemUnderTest()
        {
            QueueUniqueKey = "test" + DateTime.Now.Ticks;
            return new SqsQueueByName(QueueUniqueKey, SqsClientFactory.Create(RegionEndpoint.EUWest1), 1);
        }
        public override void PostAssertTeardown()
        {
            SystemUnderTest.Delete();
            base.PostAssertTeardown();
        }
    }
}
