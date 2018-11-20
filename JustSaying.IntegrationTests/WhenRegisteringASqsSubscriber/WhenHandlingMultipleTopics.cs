using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenHandlingMultipleTopics : WhenRegisteringASqsTopicSubscriber
    {
        protected override Task When()
        {
            SystemUnderTest.WithSqsTopicSubscriber()
                .IntoQueue(QueueName)
                .WithMessageHandler(Substitute.For<IHandlerAsync<GenericMessage<TopicA>>>())
                .WithMessageHandler(Substitute.For<IHandlerAsync<GenericMessage<TopicB>>>());

            return Task.CompletedTask;
        }

        [NotSimulatorFact]
        public async Task SqsPolicyWithAWildcardIsApplied()
        {
            var queue = new SqsQueueByName(Region, QueueName, Client, 0, TestFixture.LoggerFactory);

            await Patiently.AssertThatAsync(() => queue.ExistsAsync(), TimeSpan.FromSeconds(60));

            dynamic policyJson = JObject.Parse(queue.Policy);

            policyJson.Statement.Count.ShouldBe(1,  $"Expecting 1 statement in Sqs policy but found {policyJson.Statement.Count}");
        }

        public class TopicA : Message
        {
        }

        public class TopicB : Message
        {
        }
    }
}
