using JustEat.Testing;
using NUnit.Framework;
using SimpleMessageMule.TestingFramework;

namespace AwsTools.IntegrationTests
{
    public class WhenICreateAQueueByName : WhenCreatingQueuesByName
    {
        private bool _isQueueCreated;

        protected override void When()
        {
            _isQueueCreated = SystemUnderTest.Create(60, attempt: 0, visibilityTimeoutSeconds: 30);
        }

        [Then]
        public void TheQueueISCreated()
        {
            Assert.IsTrue(_isQueueCreated);
        }

        [Then]
        public void DeadLetterQueueIsCreated()
        {
            Patiently.AssertThat(() => SystemUnderTest.ErrorQueue.Exists(), 10.Seconds());
        }
    }
}