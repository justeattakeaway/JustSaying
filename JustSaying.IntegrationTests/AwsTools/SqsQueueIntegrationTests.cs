using System;
using Amazon;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.IntegrationTests
{
    public abstract class WhenCreatingQueuesByName : XBehaviourTest<SqsQueueByName>
    {
        protected string QueueUniqueKey;

        protected override void Given()
        { }

        protected override SqsQueueByName CreateSystemUnderTest()
        {
            QueueUniqueKey = "test" + DateTime.Now.Ticks;
            var queue = new SqsQueueByName(RegionEndpoint.EUWest1, QueueUniqueKey, CreateMeABus.DefaultClientFactory().GetSqsClient(RegionEndpoint.EUWest1), 1, new LoggerFactory());
            queue.Exists();
            return queue;
        }

        protected override void PostAssertTeardown()
        {
            SystemUnderTest.Delete();
            base.PostAssertTeardown();
        }
    }
}
