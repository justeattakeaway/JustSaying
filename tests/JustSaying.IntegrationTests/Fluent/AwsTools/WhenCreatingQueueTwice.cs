using System;
using System.Threading.Tasks;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit.Abstractions;

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

            var subjectProvider = new NonGenericMessageSubjectProvider();
            var serializerFactor = new NewtonsoftSerializationFactory();
            var serializationRegister = new MessageSerializationRegister(subjectProvider, serializerFactor);

            var client = clientFactory.GetSnsClient(Region);

            var topic = new SnsTopicByName(
                topicName,
                client,
                serializationRegister,
                loggerFactory,
                subjectProvider);

            // Act and Assert
            (await topic.CreateAsync()).ShouldBeTrue();
            (await topic.CreateAsync()).ShouldBeTrue();

            topic.Arn.ShouldNotBeNull();
            topic.Arn.ShouldEndWith(topic.TopicName);
        }
    }
}
