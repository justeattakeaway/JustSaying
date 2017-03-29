using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustBehave;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.AwsTools.IntegrationTests
{
    public class WhenInstatiatingAnSNSTopicByNameUsingTopicObject : BehaviourTest<SnsTopicByName>
    {
        protected string QueueUniqueKey;
        IAmazonSimpleNotificationService client;

        protected override void Given()
        {

            client = CreateMeABus.DefaultClientFactory().GetSnsClient(RegionEndpoint.EUWest1);
        }

        protected override SnsTopicByName CreateSystemUnderTest()
        {
            QueueUniqueKey = "test" + DateTime.Now.Ticks;
            var topic = new SnsTopicByName("TestTopic", client , new MessageSerialisationRegister(), new LoggerFactory());
            return topic;
        }

        protected override void When()
        {
            if(!SystemUnderTest.Exists()) SystemUnderTest.Create();
        }

        [Test]
        public void WillResolveCorrectTopicName()
        {
            var actualTopicCreated = ListTopics(client).Result.FirstOrDefault(x=>x.TopicArn == SystemUnderTest.Arn);

            var cachableTopicByNameObject = new SnsTopicByName(actualTopicCreated, client, new MessageSerialisationRegister(), new LoggerFactory());

            Assert.That(cachableTopicByNameObject.Arn, Is.EqualTo(SystemUnderTest.Arn));
            Assert.That(cachableTopicByNameObject.TopicName, Is.EqualTo(SystemUnderTest.TopicName));

        }

        private static async Task<List<Topic>> ListTopics(IAmazonSimpleNotificationService snsclient)
        {
            var topics = new List<Topic>();
            string nextToken = null;
            do
            {
                var listTopicsResponse = await snsclient.ListTopicsAsync(new ListTopicsRequest
                {
                    NextToken = nextToken
                });
                if (listTopicsResponse?.Topics == null || listTopicsResponse.Topics.Count == 0)
                {
                    break;
                }
                topics.AddRange(listTopicsResponse.Topics);
                nextToken = listTopicsResponse.NextToken;
            } while (!string.IsNullOrEmpty(nextToken));
            return topics;

        }

        public override void PostAssertTeardown()
        {
            client.DeleteTopic(SystemUnderTest.Arn);
            base.PostAssertTeardown();
        }
    }
}
