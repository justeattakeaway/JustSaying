using System.Threading.Tasks;
using Amazon;
using JustSaying.Extensions;
using Xunit;

namespace JustSaying.UnitTests
{
    public static class HttpClientFactoryTests
    {
        [Fact]
        public static async Task Create_SNS_Client_Using_HttpClientFactory()
        {
            // Arrange
            var factory = new HttpClientFactoryAwsClientFactory();

            // Act
            using (var client = factory.GetSnsClient(RegionEndpoint.EUWest1))
            {
                // Assert (no throw)
                await client.ListTopicsAsync();
            }
        }

        [Fact]
        public static async Task Create_SQS_Client_Using_HttpClientFactory()
        {
            // Arrange
            var factory = new HttpClientFactoryAwsClientFactory();

            // Act
            using (var client = factory.GetSqsClient(RegionEndpoint.EUWest1))
            {
                // Assert (no throw)
                await client.ListQueuesAsync("*");
            }
        }
    }
}
