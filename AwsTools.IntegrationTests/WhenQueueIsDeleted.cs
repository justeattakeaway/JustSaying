using NUnit.Framework;
using SimpleMessageMule.TestingFramework;

namespace AwsTools.IntegrationTests
{
    public class WhenQueueIsDeleted : WhenCreatingQueuesByName
    {
        protected override void When()
        {
            SystemUnderTest.Create(600);
            SystemUnderTest.Delete();
        }

        [Test]
        public void TheErrorQueueIsDeleted()
        {
            Patiently.AssertThat(() => !SystemUnderTest.ErrorQueue.Exists());
        }
    }
}