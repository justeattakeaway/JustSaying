using Amazon.SQS.Model;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

#pragma warning disable 618

namespace JustSaying.IntegrationTests.Fluent.AwsTools;

public class WhenEnsuringATopicExistsPolicyIsCreated : IntegrationTestBase
{
    public WhenEnsuringATopicExistsPolicyIsCreated(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
    }

    [AwsFact]
    public async Task Then_The_Iam_Policy_Is_Created()
    {
        // Arrange
        ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();
        IAwsClientFactory clientFactory = CreateClientFactory();
        var QueueName = UniqueName;
        var TopicName = $"{UniqueName}-topic";

        var client = clientFactory.GetSqsClient(Region);

        var queueCreator = new AmazonQueueCreator(new AwsClientFactoryProxy(() => clientFactory), loggerFactory);

        var readConfig = new SqsReadConfiguration(SubscriptionType.ToTopic)
        {
            QueueName = QueueName,
            SubscriptionGroupName = QueueName,
            TopicName = TopicName,
            PublishEndpoint = TopicName,
            RetryCountBeforeSendingToErrorQueue = 1,
        };

        var queue = queueCreator.EnsureTopicExistsWithQueueSubscribed(RegionName, readConfig);

        // Act
        await queue.StartupTask.Invoke(CancellationToken.None);

        // Assert
        var queueAttributesResponse = await client.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = queue.Queue.Uri.ToString(),
            AttributeNames = new List<string> { "Policy" }
        });
        queueAttributesResponse.Policy.ShouldMatchApproved(opt =>
        {
            opt.SubFolder("Approvals");

            opt.WithScrubber(policy => ScrubUniqueData(policy, UniqueName, RegionName));
        });
    }

    private static string ScrubUniqueData(string iamPolicy, string uniqueName, string regionName)
    {
        var json = JObject.Parse(iamPolicy);
        var sourceArn = json["Statement"]![0]!["Condition"]!["ArnLike"]!["aws:SourceArn"]!.ToString();
        var resourceArn = json["Statement"]![0]!["Resource"]!.ToString();
        return iamPolicy
            .Replace(json["Statement"]![0]!["Sid"]!.ToString(), "<sid>")
            .Replace(resourceArn, resourceArn.Replace(uniqueName, "<unique-name>"))
            .Replace(sourceArn, sourceArn.Replace(uniqueName, "<unique-name>"));
    }
}
