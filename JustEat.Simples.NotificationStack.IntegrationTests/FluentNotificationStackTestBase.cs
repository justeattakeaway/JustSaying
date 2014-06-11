using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Amazon;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using JustEat.Testing;
using JustSaying;
using JustSaying.Stack;
using NSubstitute;

namespace NotificationStack.IntegrationTests
{
    public abstract class FluentNotificationStackTestBase : BehaviourTest<FluentNotificationStack>
    {
        protected INotificationStackConfiguration Configuration;
        protected IAmJustSaying NotificationStack { get; private set; }
        private bool _mockNotificationStack;
        protected const int QueueCreationDelayMilliseconds = 10 * 1000;
        
        protected override void Given()
        {
            throw new NotImplementedException();
        }

        protected override FluentNotificationStack CreateSystemUnderTest()
        {
            var fns = FluentNotificationStack.Register(x =>
            {
                x.Component = Configuration.Component;
                x.Environment = Configuration.Environment;
                x.PublishFailureBackoffMilliseconds = Configuration.PublishFailureBackoffMilliseconds;
                x.PublishFailureReAttempts = Configuration.PublishFailureReAttempts;
                x.Region = Configuration.Region;
                x.Tenant = Configuration.Tenant;
            }).WithMonitoring(null) as FluentNotificationStack;

            if (_mockNotificationStack)
            {
                NotificationStack = Substitute.For<IAmJustSaying>();

                var notificationStackField = fns.GetType().GetField("_stack", BindingFlags.Instance | BindingFlags.NonPublic);

                var constructedStack = (IAmJustSaying)notificationStackField.GetValue(fns);

                NotificationStack.Config.Returns(constructedStack.Config);

                notificationStackField.SetValue(fns, NotificationStack);
            }

            return fns;
        }

        protected override void When()
        {
            throw new NotImplementedException();
        }

        public void MockNotidicationStack()
        {
            _mockNotificationStack = true;
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

            throw new Exception(string.Format("Deleted queue still exists {0} seconds after deletion!", (DateTime.Now - start).TotalSeconds));
        }

        protected static void DeleteTopic(RegionEndpoint regionEndpoint, Topic topic)
        {
            var client = AWSClientFactory.CreateAmazonSimpleNotificationServiceClient(regionEndpoint);
            client.DeleteTopic(new DeleteTopicRequest { TopicArn = topic.TopicArn });
        }

        private static void DeleteQueue(RegionEndpoint regionEndpoint, string queueUrl)
        {
            var client = AWSClientFactory.CreateAmazonSQSClient(regionEndpoint);
            client.DeleteQueue(new DeleteQueueRequest { QueueUrl = queueUrl });
        }

        private static List<Topic> GetAllTopics(RegionEndpoint regionEndpoint, string topicName)
        {
            var client = AWSClientFactory.CreateAmazonSimpleNotificationServiceClient(regionEndpoint);
            var topics = client.ListTopics(new ListTopicsRequest());
            return topics.Topics.Where(x => x.TopicArn.IndexOf(topicName, StringComparison.InvariantCultureIgnoreCase) >= 0).ToList();
        }

        private static List<string> GetAllQueues(RegionEndpoint regionEndpoint, string queueName)
        {
            var client = AWSClientFactory.CreateAmazonSQSClient(regionEndpoint);
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

            var sqsclient = AWSClientFactory.CreateAmazonSQSClient(regionEndpoint);

            var queueArn = sqsclient.GetQueueAttributes(request).QueueARN;

            var client = AWSClientFactory.CreateAmazonSimpleNotificationServiceClient(regionEndpoint);

            var subscriptions =  client.ListSubscriptionsByTopic(new ListSubscriptionsByTopicRequest(topic.TopicArn)).Subscriptions;

            return subscriptions.Any(x => !string.IsNullOrEmpty(x.SubscriptionArn) && x.Endpoint == queueArn);
        }

        protected bool QueueHasPolicyForTopic(RegionEndpoint regionEndpoint, Topic topic, string queueUrl)
        {
            var client = AWSClientFactory.CreateAmazonSQSClient(regionEndpoint);

            var policy = client.GetQueueAttributes(new GetQueueAttributesRequest{ QueueUrl = queueUrl, AttributeNames = new List<string>{ "Policy" }}).Policy;

            return policy.Contains(topic.TopicArn);
        }
    }
}