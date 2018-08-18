using System.Threading.Tasks;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests.AwsTools
{
    public abstract class WhenCreatingQueuesByName : XAsyncBehaviourTest<SqsQueueByName>
    {
        protected override void Given()
        {
        }

        protected override SqsQueueByName CreateSystemUnderTest()
        {
            var fixture = new JustSayingFixture();

            var queue = new SqsQueueByName(
                fixture.Region,
                fixture.UniqueName,
                fixture.CreateSqsClient(),
                1,
                fixture.LoggerFactory);

            // Force queue creation
            queue.ExistsAsync().ResultSync();

            return queue;
        }

        protected override void PostAssertTeardown()
        {
            SystemUnderTest.DeleteAsync().ResultSync();
            base.PostAssertTeardown();
        }
    }
}
