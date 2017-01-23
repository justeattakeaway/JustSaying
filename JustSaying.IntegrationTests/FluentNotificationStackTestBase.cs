using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using JustBehave;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.IntegrationTests
{
    public abstract class FluentNotificationStackTestBase : BehaviourTest<JustSaying.JustSayingFluently>
    {
        private static readonly RegionEndpoint DefaultEndpoint = RegionEndpoint.EUWest1;
        protected static RegionEndpoint TestEndpoint { get; set; }

        protected IPublishConfiguration Configuration;
        protected IAmJustSaying NotificationStack { get; private set; }
        private bool _enableMockedBus;
        
        protected override void Given()
        {
            TestEndpoint = DefaultEndpoint;
        }

        protected override JustSaying.JustSayingFluently CreateSystemUnderTest()
        {
            var fns = CreateMeABus.WithLogging(new LoggerFactory()).InRegion(TestEndpoint.SystemName)
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

        protected override void When()
        {
            throw new NotImplementedException();
        }

        protected void EnableMockedBus()
        {
            _enableMockedBus = true;
        }

        public static void DeleteTopicIfItAlreadyExists(string regionEndpointName, string topicName)
        {
            DeleteTopicIfItAlreadyExists(RegionEndpoint.GetBySystemName(regionEndpointName), topicName);
        }

        public static void DeleteTopicIfItAlreadyExists(RegionEndpoint regionEndpoint, string topicName)
        {            
            var topics = GetAllTopics(regionEndpoint, topicName);
            
            topics.ForEach(t => DeleteTopic(regionEndpoint, t));

            Topic topic;
            if (TryGetTopic(regionEndpoint, topicName, out topic))
            {
                throw new Exception("Deleted topic still exists!");
            }
        }

        protected void DeleteQueueIfItAlreadyExists(RegionEndpoint regionEndpoint, string queueName)
        {
            var queues = GetAllQueues(regionEndpoint, queueName);

            queues.ForEach(t => DeleteQueue(regionEndpoint, t));

            const int maxSleepTime = 60;
            const int sleepStep = 5;

            var start = DateTime.Now;

            while ((DateTime.Now - start).TotalSeconds <= maxSleepTime)
            {
                if (!GetAllQueues(regionEndpoint, queueName).Any())
                    return;

                Thread.Sleep(TimeSpan.FromSeconds(sleepStep));
            }

            throw new Exception(
                $"Deleted queue still exists {(DateTime.Now - start).TotalSeconds} seconds after deletion!");
        }

        // ToDo: All these can go because we have already implemented them in AwsTools... Seriously. Wasted effort.

        protected static void DeleteTopic(RegionEndpoint regionEndpoint, Topic topic)
        {
            var client = CreateMeABus.DefaultClientFactory().GetSnsClient(regionEndpoint);
            client.DeleteTopic(new DeleteTopicRequest { TopicArn = topic.TopicArn });
        }

        private static void DeleteQueue(RegionEndpoint regionEndpoint, string queueUrl)
        {
            var client = CreateMeABus.DefaultClientFactory().GetSqsClient(regionEndpoint);
            client.DeleteQueue(new DeleteQueueRequest { QueueUrl = queueUrl });
        }

        private static List<Topic> GetAllTopics(RegionEndpoint regionEndpoint, string topicName)
        {
            var client = CreateMeABus.DefaultClientFactory().GetSnsClient(regionEndpoint);
            var topics = new List<Topic>();
            string nextToken = null;
            do
            {
                var topicsResponse = client.ListTopics(new ListTopicsRequest{NextToken = nextToken});
                nextToken = topicsResponse.NextToken;
                topics.AddRange(topicsResponse.Topics);
            } while (nextToken != null);

            return topics.Where(x => x.TopicArn.IndexOf(topicName, StringComparison.InvariantCultureIgnoreCase) >= 0).ToList();
        }

        private static List<string> GetAllQueues(RegionEndpoint regionEndpoint, string queueName)
        {
            var client = CreateMeABus.DefaultClientFactory().GetSqsClient(regionEndpoint);
            var topics = client.ListQueues(new ListQueuesRequest());
            return topics.QueueUrls.Where(x => x.IndexOf(queueName, StringComparison.InvariantCultureIgnoreCase) >= 0).ToList();
        }

        protected static bool TryGetTopic(RegionEndpoint regionEndpoint, string topicName, out Topic topic)
        {
            topic = GetAllTopics(regionEndpoint, topicName).SingleOrDefault();

            return topic != null;
        }

        protected static bool WaitForQueueToExist(RegionEndpoint regionEndpoint, string queueName, out string queueUrl)
        {
            const int maxSleepTime = 60;
            const int sleepStep = 5;
            
            var start = DateTime.Now;

            while ((DateTime.Now - start).TotalSeconds <= maxSleepTime) 
            {
                queueUrl = GetAllQueues(regionEndpoint, queueName).FirstOrDefault();

                if (!String.IsNullOrEmpty(queueUrl))
                    return true;

                Thread.Sleep(TimeSpan.FromSeconds(sleepStep));
            }

            queueUrl = null;
            return false;
        }

        protected bool IsQueueSubscribedToTopic(RegionEndpoint regionEndpoint, Topic topic, string queueUrl)
        {
            var request = new GetQueueAttributesRequest{ QueueUrl = queueUrl, AttributeNames = new List<string> { "QueueArn" } };

            var sqsclient = CreateMeABus.DefaultClientFactory().GetSqsClient(regionEndpoint);

            var queueArn = sqsclient.GetQueueAttributes(request).QueueARN;

            var client = new AmazonSimpleNotificationServiceClient(regionEndpoint);

            var subscriptions =  client.ListSubscriptionsByTopic(new ListSubscriptionsByTopicRequest(topic.TopicArn)).Subscriptions;

            return subscriptions.Any(x => !string.IsNullOrEmpty(x.SubscriptionArn) && x.Endpoint == queueArn);
        }

        protected bool QueueHasPolicyForTopic(RegionEndpoint regionEndpoint, Topic topic, string queueUrl)
        {
            var client = CreateMeABus.DefaultClientFactory().GetSqsClient(regionEndpoint);

            var policy = client.GetQueueAttributes(new GetQueueAttributesRequest{ QueueUrl = queueUrl, AttributeNames = new List<string>{ "Policy" }}).Policy;

            return policy.Contains(topic.TopicArn);
        }
    }
}