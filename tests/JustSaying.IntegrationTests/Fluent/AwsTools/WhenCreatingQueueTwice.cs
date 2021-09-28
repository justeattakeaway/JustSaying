using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit.Abstractions;
#pragma warning disable 618

namespace JustSaying.IntegrationTests.Fluent.AwsTools
{
    public class WhenCreatingQueueTwice : IntegrationTestBase
    {
        public WhenCreatingQueueTwice(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

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
}
