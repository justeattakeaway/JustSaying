using JustBehave;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenIAccessAnExistingQueueWithoutAnErrorQueue : WhenCreatingQueuesByName
    {
        protected override void When()
        {
            SystemUnderTest.Create(new SqsBasicConfiguration(), attempt: 0);
        }

        [Then]
        public void ThereIsNoErrorQueue()
        {
            Patiently.AssertThat(() => !SystemUnderTest.ErrorQueue.Exists());
        }
    }
}