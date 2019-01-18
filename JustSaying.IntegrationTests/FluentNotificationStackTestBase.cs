using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.IntegrationTests
{
    public abstract class FluentNotificationStackTestBase : XAsyncBehaviourTest<JustSaying.JustSayingFluently>
    {
        protected RegionEndpoint Region => TestFixture.Region;

        protected IPublishConfiguration Configuration { get; set; }

        protected IAmJustSaying NotificationStack { get; private set; }

        protected JustSayingFixture TestFixture { get; } = new JustSayingFixture();

        private bool _enableMockedBus;

        protected override Task Given() => Task.CompletedTask;

        protected override Task<JustSaying.JustSayingFluently> CreateSystemUnderTestAsync()
        {
            var fluent = TestFixture.Builder()
                .ConfigurePublisherWith(x =>
                {
                    x.PublishFailureBackoff = Configuration.PublishFailureBackoff;
                    x.PublishFailureReAttempts = Configuration.PublishFailureReAttempts;
                }) as JustSaying.JustSayingFluently;

            if (_enableMockedBus)
            {
                InjectMockJustSayingBus(fluent);
            }

            return Task.FromResult(fluent);
        }

        private void InjectMockJustSayingBus(JustSaying.JustSayingFluently fluent)
        {
            var constructedStack = (JustSayingBus)fluent.Bus;

            NotificationStack = Substitute.For<IAmJustSaying>();
            NotificationStack.Config.Returns(constructedStack.Config);

            fluent.Bus = NotificationStack;
        }

        protected override Task When() => Task.CompletedTask;

        protected void EnableMockedBus()
        {
            _enableMockedBus = true;
        }

        protected async Task DeleteTopicIfItAlreadyExists(string topicName)
        {
            var topics = await GetAllTopics(topicName).ConfigureAwait(false);

            await Task.WhenAll(topics.Select(DeleteTopicAsync)).ConfigureAwait(false);

            var (topicExists, _) = await TryGetTopic(topicName).ConfigureAwait(false);

            if (topicExists)
            {
                throw new Exception("Deleted topic still exists!");
            }
        }

        protected async Task DeleteQueueIfItAlreadyExists(string queueName)
        {
            var queues = await GetAllQueues(queueName).ConfigureAwait(false);

            foreach (var queue in queues)
            {
                await DeleteQueue(queue);
            }

            bool isSimulator = TestEnvironment.IsSimulatorConfigured;
            int maxSleepTime = isSimulator ? 10 : 60;
            int sleepStep = isSimulator ? 1 : 5;

            var start = DateTime.Now;

            while ((DateTime.Now - start).TotalSeconds <= maxSleepTime)
            {
                if (!(await GetAllQueues(queueName).ConfigureAwait(false)).Any())
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(sleepStep)).ConfigureAwait(false);
            }

            throw new Exception($"Deleted queue still exists {(DateTime.Now - start).TotalSeconds} seconds after deletion!");
        }

        protected async Task DeleteTopicAsync(Topic topic)
        {
            var client = TestFixture.CreateSnsClient();
            await client.DeleteTopicAsync(topic.TopicArn).ConfigureAwait(false);
        }

        private async Task DeleteQueue(string queueUrl)
        {
            var client = TestFixture.CreateSqsClient();
            await client.DeleteQueueAsync(queueUrl).ConfigureAwait(false);
        }

        private async Task<List<Topic>> GetAllTopics(string topicName)
        {
            var client = TestFixture.CreateSnsClient();
            var topics = new List<Topic>();
            string nextToken = null;

            do
            {
                var topicsResponse = await client.ListTopicsAsync(nextToken).ConfigureAwait(false);
                nextToken = topicsResponse.NextToken;
                topics.AddRange(topicsResponse.Topics);
            }
            while (nextToken != null);

            return topics
                .Where(x => x.TopicArn.IndexOf(topicName, StringComparison.InvariantCultureIgnoreCase) >= 0)
                .ToList();
        }

        private async Task<List<string>> GetAllQueues(string queueName)
        {
            var client = TestFixture.CreateSqsClient();
            var topics = await client.ListQueuesAsync(new ListQueuesRequest()).ConfigureAwait(false);

            return topics.QueueUrls
                .Where(x => x.IndexOf(queueName, StringComparison.InvariantCultureIgnoreCase) >= 0)
                .ToList();
        }

        protected async Task<(bool topicExists, Topic topic)> TryGetTopic(string topicName)
        {
            var topic = (await GetAllTopics(topicName).ConfigureAwait(false)).SingleOrDefault();

            return (topic != null, topic);
        }

        protected async Task<(bool queueExists,string queueUrl)> WaitForQueueToExist(string queueName)
        {
            bool isSimulator = TestEnvironment.IsSimulatorConfigured;
            int maxSleepTime = isSimulator ? 10 : 60;
            int sleepStep = isSimulator ? 1 : 5;

            var start = DateTime.Now;

            while ((DateTime.Now - start).TotalSeconds <= maxSleepTime)
            {
                var queueUrl = (await GetAllQueues(queueName).ConfigureAwait(false)).FirstOrDefault();

                if (!string.IsNullOrEmpty(queueUrl))
                {
                    return (true, queueUrl);
                }

                await Task.Delay(TimeSpan.FromSeconds(sleepStep)).ConfigureAwait(false);
            }

            return (false, null);
        }

// disable warning about "queueUrl" typed as string
#pragma warning disable CA1054
        protected async Task<bool> IsQueueSubscribedToTopic(Topic topic, string queueUrl)
#pragma warning restore CA1054
        {
            var request = new GetQueueAttributesRequest
            {
                QueueUrl = queueUrl,
                AttributeNames = new List<string> { "QueueArn" }
            };

            var sqsclient = TestFixture.CreateSqsClient();

            var queueArn = (await sqsclient.GetQueueAttributesAsync(request).ConfigureAwait(false)).QueueARN;

            var snsClient = TestFixture.CreateSnsClient();

            var subscriptions =
                (await snsClient.ListSubscriptionsByTopicAsync(topic.TopicArn).ConfigureAwait(false)).Subscriptions;

            return subscriptions.Any(x => !string.IsNullOrEmpty(x.SubscriptionArn) && x.Endpoint == queueArn);
        }

        // disable warning about "queueUrl" typed as string
#pragma warning disable CA1054
        protected async Task<bool> QueueHasPolicyForTopic(Topic topic, string queueUrl)
#pragma warning restore CA1054
        {
            var client = TestFixture.CreateSqsClient();

            var policy = (await client.GetQueueAttributesAsync(queueUrl, new List<string> { "Policy" }).ConfigureAwait(false)).Policy;

            int pos = topic.TopicArn.LastIndexOf(':');
            string wildcardedSubscription = topic.TopicArn.Substring(0, pos + 1) + "*";

            return policy.Contains(topic.TopicArn, StringComparison.OrdinalIgnoreCase) ||
                   policy.Contains(wildcardedSubscription, StringComparison.OrdinalIgnoreCase);
        }
    }
}
