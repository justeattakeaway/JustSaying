using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;

#pragma warning disable 618

namespace JustSaying.IntegrationTests.Fluent.AwsTools;

public class WhenCreatingQueueTwice(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_An_Exception_Is_Not_Thrown()
    {
        // Arrange
        string topicName = Guid.NewGuid().ToString();
        ILoggerFactory loggerFactory = OutputHelper.ToLoggerFactory();
        IAwsClientFactory clientFactory = CreateClientFactory();

        var client = clientFactory.GetSnsClient(Region);

        var topic = new SnsTopicByName(
            topicName,
            client,
            loggerFactory);

        // Shouldn't throw
        await topic.CreateAsync(CancellationToken.None);
        await topic.CreateAsync(CancellationToken.None);

        topic.Arn.ShouldNotBeNull();
        topic.Arn.ShouldEndWith(topic.TopicName);
    }
}