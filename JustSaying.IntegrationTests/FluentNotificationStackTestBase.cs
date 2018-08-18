using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.IntegrationTests
{
    public abstract class FluentNotificationStackTestBase : XAsyncBehaviourTest<JustSaying.JustSayingFluently>
    {
        private static RegionEndpoint DefaultEndpoint => TestEnvironment.Region;

        protected static RegionEndpoint TestEndpoint { get; set; }

        protected IPublishConfiguration Configuration { get; set; }

        protected IAmJustSaying NotificationStack { get; private set; }

        private bool _enableMockedBus;

        protected override void Given()
        {
            TestEndpoint = DefaultEndpoint;
        }

        protected override JustSaying.JustSayingFluently CreateSystemUnderTest()
        {
            var fns = CreateMeABus
                .WithLogging(new LoggerFactory())
                .InRegion(TestEndpoint.SystemName)
                .ConfigurePublisherWith(x =>
                {
                    x.PublishFailureBackoffMilliseconds = Configuration.PublishFailureBackoffMilliseconds;
                    x.PublishFailureReAttempts = Configuration.PublishFailureReAttempts;
                }) as JustSaying.JustSayingFluently;

            if (_enableMockedBus)
            {
                InjectMockJustSayingBus(fns);
            }

            return fns;
        }

        private void InjectMockJustSayingBus(JustSaying.JustSayingFluently fns)
        {
            NotificationStack = Substitute.For<IAmJustSaying>();

            var notificationStackField = fns.GetType().GetField("Bus", BindingFlags.Instance | BindingFlags.NonPublic);

            var constructedStack = (JustSayingBus) notificationStackField.GetValue(fns);

            NotificationStack.Config.Returns(constructedStack.Config);

            notificationStackField.SetValue(fns, NotificationStack);
        }

        protected override Task When()
        {
            throw new NotImplementedException();
        }

        protected void EnableMockedBus()
        {
            _enableMockedBus = true;
        }

        public static async Task DeleteTopicIfItAlreadyExists(string regionEndpointName, string topicName)
        {
            await DeleteTopicIfItAlreadyExists(RegionEndpoint.GetBySystemName(regionEndpointName), topicName);
        }

        public static async Task DeleteTopicIfItAlreadyExists(RegionEndpoint regionEndpoint, string topicName)
        {
            var topics = await GetAllTopics(regionEndpoint, topicName);
            
            await Task.WhenAll(topics.Select(t => DeleteTopic(regionEndpoint, t)));

            var (topicExists, _) = await TryGetTopic(regionEndpoint, topicName);

            if (topicExists)
            {
                throw new Exception("Deleted topic still exists!");
            }
        }

        protected async Task DeleteQueueIfItAlreadyExists(RegionEndpoint regionEndpoint, string queueName)
        {
            var queues = await GetAllQueues(regionEndpoint, queueName);

            queues.ForEach(t => DeleteQueue(regionEndpoint, t).Wait());

            bool isSimulator = TestEnvironment.IsSimulatorConfigured;
            int maxSleepTime = isSimulator ? 10 : 60;
            int sleepStep = isSimulator ? 1 : 5;

            var start = DateTime.Now;

            while ((DateTime.Now - start).TotalSeconds <= maxSleepTime)
            {
                if (!(await GetAllQueues(regionEndpoint, queueName)).Any())
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(sleepStep));
            }

            throw new Exception($"Deleted queue still exists {(DateTime.Now - start).TotalSeconds} seconds after deletion!");
        }

        // TODO: All these can go because we have already implemented them in AwsTools... Seriously. Wasted effort.

        protected static async Task DeleteTopic(RegionEndpoint regionEndpoint, Topic topic)
        {
            var client = CreateMeABus.DefaultClientFactory().GetSnsClient(regionEndpoint);
            await client.DeleteTopicAsync(new DeleteTopicRequest { TopicArn = topic.TopicArn });
        }

        private static async Task DeleteQueue(RegionEndpoint regionEndpoint, string queueUrl)
        {
            var client = CreateMeABus.DefaultClientFactory().GetSqsClient(regionEndpoint);
            await client.DeleteQueueAsync(new DeleteQueueRequest { QueueUrl = queueUrl });
        }

        private static async Task<List<Topic>> GetAllTopics(RegionEndpoint regionEndpoint, string topicName)
        {
            var client = CreateMeABus.DefaultClientFactory().GetSnsClient(regionEndpoint);
            var topics = new List<Topic>();
            string nextToken = null;

            do
            {
                var topicsResponse = await client.ListTopicsAsync(new ListTopicsRequest { NextToken = nextToken });
                nextToken = topicsResponse.NextToken;
                topics.AddRange(topicsResponse.Topics);
            }
            while (nextToken != null);

            return topics
                .Where(x => x.TopicArn.IndexOf(topicName, StringComparison.InvariantCultureIgnoreCase) >= 0)
                .ToList();
        }

        private static async Task<List<string>> GetAllQueues(RegionEndpoint regionEndpoint, string queueName)
        {
            var client = CreateMeABus.DefaultClientFactory().GetSqsClient(regionEndpoint);
            var topics = await client.ListQueuesAsync(new ListQueuesRequest());

            return topics.QueueUrls
                .Where(x => x.IndexOf(queueName, StringComparison.InvariantCultureIgnoreCase) >= 0)
                .ToList();
        }

        protected static async Task<(bool topicExists, Topic topic)> TryGetTopic(RegionEndpoint regionEndpoint, string topicName)
        {
            var topic = (await GetAllTopics(regionEndpoint, topicName)).SingleOrDefault();

            return (topic != null, topic);
        }

        protected static async Task<(bool queueExists,string queueUrl)> WaitForQueueToExist(RegionEndpoint regionEndpoint, string queueName)
        {
            bool isSimulator = TestEnvironment.IsSimulatorConfigured;
            int maxSleepTime = isSimulator ? 10 : 60;
            int sleepStep = isSimulator ? 1 : 5;

            var start = DateTime.Now;

            while ((DateTime.Now - start).TotalSeconds <= maxSleepTime)
            {
                var queueUrl = (await GetAllQueues(regionEndpoint, queueName)).FirstOrDefault();

                if (!string.IsNullOrEmpty(queueUrl))
                {
                    return (true, queueUrl);
                }

                await Task.Delay(TimeSpan.FromSeconds(sleepStep));
            }
            
            return (false, null);
        }

        protected async Task<bool> IsQueueSubscribedToTopic(RegionEndpoint regionEndpoint, Topic topic, string queueUrl)
        {
            var request = new GetQueueAttributesRequest
            {
                QueueUrl = queueUrl,
                AttributeNames = new List<string> { "QueueArn" }
            };

            var sqsclient = CreateMeABus.DefaultClientFactory().GetSqsClient(regionEndpoint);

            var queueArn = (await sqsclient.GetQueueAttributesAsync(request)).QueueARN;

            var client = new AmazonSimpleNotificationServiceClient(regionEndpoint);

            var subscriptions =
                (await client.ListSubscriptionsByTopicAsync(new ListSubscriptionsByTopicRequest(topic.TopicArn))).Subscriptions;

            return subscriptions.Any(x => !string.IsNullOrEmpty(x.SubscriptionArn) && x.Endpoint == queueArn);
        }

        protected async Task<bool> QueueHasPolicyForTopic(RegionEndpoint regionEndpoint, Topic topic, string queueUrl)
        {
            var client = CreateMeABus.DefaultClientFactory().GetSqsClient(regionEndpoint);

            var policy =
                (await client.GetQueueAttributesAsync(new GetQueueAttributesRequest
                {
                    QueueUrl = queueUrl,
                    AttributeNames = new List<string> { "Policy" }
                })).Policy;

            int pos = topic.TopicArn.LastIndexOf(':');
            string wildcardedSubscription = topic.TopicArn.Substring(0, pos + 1) + "*";

            return policy.Contains(topic.TopicArn) || policy.Contains(wildcardedSubscription);
        }
    }
}
