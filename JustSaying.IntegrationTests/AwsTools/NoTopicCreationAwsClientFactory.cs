using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using JustSaying.AwsTools;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace JustSaying.IntegrationTests.AwsTools
{
    public class NoTopicCreationAwsClientFactory : IAwsClientFactory
    {

        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        {
            var innerClient = CreateMeABus.DefaultClientFactory().GetSnsClient(region);
            var client = Substitute.For<IAmazonSimpleNotificationService>();

            client.CreateTopicAsync(Arg.Any<CreateTopicRequest>())
                .ThrowsForAnyArgs(x => new AuthorizationErrorException("Denied"));

            client.FindTopicAsync(Arg.Any<string>())
                .ReturnsForAnyArgs(r => innerClient.FindTopicAsync(r.Arg<string>()));

            client.GetTopicAttributesAsync(Arg.Any<string>())
                .ReturnsForAnyArgs(r => innerClient.GetTopicAttributesAsync(r.Arg<string>()));

            return client;
        }


        public IAmazonSQS GetSqsClient(RegionEndpoint region)
        {
            var innerClient = CreateMeABus.DefaultClientFactory().GetSqsClient(region);
            return innerClient;
        }
    }
}
