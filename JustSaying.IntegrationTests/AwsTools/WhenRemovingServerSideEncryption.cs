using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenRemovingServerSideEncryption : WhenCreatingQueuesByName
    {
        protected override async Task When()
        {
            await SystemUnderTest.CreateAsync(new SqsBasicConfiguration { ServerSideEncryption = new ServerSideEncryption() });

            await SystemUnderTest.UpdateQueueAttributeAsync(new SqsBasicConfiguration { ServerSideEncryption = null });
        }

        [Fact]
        public void TheServerSideEncryptionIsUpdatedWithTheNewValue()
        {
            SystemUnderTest.ServerSideEncryption.ShouldBeNull();
        }
    }
}
