using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.AwsTools
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenAddingServerSideEncryption : WhenCreatingQueuesByName
    {
        protected override async Task When()
        {
            await SystemUnderTest.CreateAsync(new SqsBasicConfiguration());

            await SystemUnderTest.UpdateQueueAttributeAsync(
                new SqsBasicConfiguration { ServerSideEncryption = new ServerSideEncryption() });
        }

        [Fact]
        public void TheServerSideEncryptionIsUpdatedWithTheNewValue()
        {
            SystemUnderTest.ServerSideEncryption.KmsMasterKeyId.ShouldBe("alias/aws/sqs");
        }
    }
}
