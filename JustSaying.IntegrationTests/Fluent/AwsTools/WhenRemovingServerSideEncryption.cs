using System.Threading.Tasks;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.AwsTools
{
    public class WhenRemovingServerSideEncryption : IntegrationTestBase
    {
        public WhenRemovingServerSideEncryption(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Can_Remove_Encryption()
        {
            // Arrange
            ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();
            IAwsClientFactory clientFactory = CreateClientFactory();

            var client = clientFactory.GetSqsClient(Region);

            var queue = new SqsQueueByName(
                Region,
                UniqueName,
                client,
                1,
                loggerFactory);

            await queue.CreateAsync(
                new SqsBasicConfiguration { ServerSideEncryption = new ServerSideEncryption() });

            // Act
            await queue.UpdateQueueAttributeAsync(
                new SqsBasicConfiguration { ServerSideEncryption = null });

            // Assert
            queue.ServerSideEncryption.ShouldBeNull();
        }
    }
}
