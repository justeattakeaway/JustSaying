using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenQueueIsDeleted : WhenCreatingQueuesByName
    {
        protected override void When()
        {
            SystemUnderTest.Create(new SqsConfiguration(), attempt:600);
            SystemUnderTest.Delete();
        }

        [Test]
        public void TheErrorQueueIsDeleted()
        {
            Patiently.AssertThat(() => !SystemUnderTest.ErrorQueue.Exists());
        }
    }
}