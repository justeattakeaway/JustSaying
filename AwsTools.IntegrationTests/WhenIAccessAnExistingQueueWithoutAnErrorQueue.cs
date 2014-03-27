using JustEat.Testing;
using SimpleMessageMule.TestingFramework;

namespace AwsTools.IntegrationTests
{
    public class WhenIAccessAnExistingQueueWithoutAnErrorQueue : WhenCreatingQueuesByName
    {
        protected override void When()
        {
            SystemUnderTest.Create(600, attempt: 0, visibilityTimeoutSeconds: 30, createErrorQueue: false);
        }

        [Then]
        public void ThereIsNoErrorQueue()
        {
            Patiently.AssertThat(() => !SystemUnderTest.ErrorQueue.Exists());
        }
    }
}