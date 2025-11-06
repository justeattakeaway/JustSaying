using System;
using System.Threading.Tasks;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit.Abstractions;
#pragma warning disable 618

namespace JustSaying.IntegrationTests.Fluent.AwsTools
{
    public class WhenRemovingSnsServerSideEncryption : IntegrationTestBase
    {
        public WhenRemovingSnsServerSideEncryption(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [NotSimulatorFact]
        public async Task Can_Remove_Encryption()
        {
            // Arrange
            ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();
            IAwsClientFactory clientFactory = CreateClientFactory();

            var client = clientFactory.GetSnsClient(Region);

            var topic = new SnsTopicByName(
                UniqueName,
                client,
                null,
                loggerFactory,
                null);

            await topic.CreateWithEncryptionAsync(new ServerSideEncryption { KmsMasterKeyId = JustSayingConstants.DefaultSnsAttributeEncryptionKeyId });

            // Act
            await topic.CreateWithEncryptionAsync(new ServerSideEncryption { KmsMasterKeyId = String.Empty });

            // Assert
            topic.ServerSideEncryption.ShouldBeNull();
        }
    }
}
