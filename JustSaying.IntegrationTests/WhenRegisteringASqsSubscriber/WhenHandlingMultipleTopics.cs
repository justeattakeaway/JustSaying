using System;
using System.Threading.Tasks;
using Amazon;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.WhenRegisteringASqsSubscriber
{

    public class WhenHandlingMultipleTopics : WhenRegisteringASqsTopicSubscriber
    {
        public class TopicA : Message { }
        public class TopicB : Message { }

        protected override void When()
        {
            SystemUnderTest.WithSqsTopicSubscriber()
                .IntoQueue(QueueName)
                .WithMessageHandler(Substitute.For<IHandlerAsync<GenericMessage<TopicA>>>())
                .WithMessageHandler(Substitute.For<IHandlerAsync<GenericMessage<TopicB>>>());
        }

        [Then]
        public async Task SqsPolicyWithAWildcardIsApplied()
        {
            var queue = new SqsQueueByName(RegionEndpoint.EUWest1, QueueName, Client, 0, Substitute.For<ILoggerFactory>());
            await Patiently.AssertThatAsync(() => queue.ExistsAsync(), TimeSpan.FromSeconds(60));
            dynamic policyJson = JObject.Parse(queue.Policy);
            Assert.IsTrue(policyJson.Statement.Count == 1, $"Expecting 1 statement in Sqs policy but found {policyJson.Statement.Count}");
        }
    }
}
