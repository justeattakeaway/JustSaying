using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class NoTopicCreationAwsClientFactory : IAwsClientFactory
    {

        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        {
            var innerClient = CreateMeABus.DefaultClientFactory().GetSnsClient(region);
            var client = Substitute.For<IAmazonSimpleNotificationService>();

            client.CreateTopic(Arg.Any<CreateTopicRequest>())
                .ThrowsForAnyArgs(x => new AuthorizationErrorException("Denied"));

            client.FindTopic(Arg.Any<string>())
                .ReturnsForAnyArgs(r => innerClient.FindTopic(r.Arg<string>()));

            client.GetTopicAttributes(Arg.Any<string>())
                .ReturnsForAnyArgs(r => innerClient.GetTopicAttributes(r.Arg<string>()));

            return client;
        }


        public IAmazonSQS GetSqsClient(RegionEndpoint region)
        {
            var innerClient = CreateMeABus.DefaultClientFactory().GetSqsClient(region);
            return innerClient;
        }
    }
}
