using System;
using Amazon;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;

namespace JustSaying.IntegrationTests.AwsTools
{
    public abstract class WhenCreatingQueuesByName : XAsyncBehaviourTest<SqsQueueByName>
    {
        protected override void Given()
        {
        }

        protected override SqsQueueByName CreateSystemUnderTest()
        {
            string queueName = "test" + DateTime.Now.Ticks;
            RegionEndpoint region = TestEnvironment.Region;

            var queue = new SqsQueueByName(
                region,
                queueName,
                CreateMeABus.DefaultClientFactory().GetSqsClient(region),
                1,
                new LoggerFactory());

            // Force queue creation
            queue.ExistsAsync().GetAwaiter().GetResult();

            return queue;
        }

        protected override void PostAssertTeardown()
        {
            SystemUnderTest.DeleteAsync().GetAwaiter().GetResult();
            base.PostAssertTeardown();
        }
    }
}
