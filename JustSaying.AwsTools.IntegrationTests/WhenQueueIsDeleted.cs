using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using NUnit.Framework;
using JustSaying.TestingFramework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenQueueIsDeleted : WhenCreatingQueuesByName
    {
        protected override void When()
        {
            SystemUnderTest.Create(
                new SqsReadConfiguration(SubscriptionType.ToTopic), 
                attempt:600);
            SystemUnderTest.Delete();
        }

        [Test]
        public async Task TheErrorQueueIsDeleted()
        {
            await Patiently.AssertThatAsync(
                () => !SystemUnderTest.ErrorQueue.Exists());
        }
    }
}