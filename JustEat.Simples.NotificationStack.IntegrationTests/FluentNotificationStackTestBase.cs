using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amazon;
using Amazon.SimpleNotificationService.Model;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.Lookups;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;

namespace NotificationStack.IntegrationTests
{
    public abstract class FluentNotificationStackTestBase : BehaviourTest<FluentNotificationStack>
    {
        protected INotificationStackConfiguration Configuration;
        protected INotificationStack NotificationStack { get; private set; }
        private bool _mockNotificationStack;

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
                NotificationStack = Substitute.For<INotificationStack>();

                var notificationStackField = fns.GetType().GetField("_stack", BindingFlags.Instance | BindingFlags.NonPublic);

                var constructedStack = (JustEat.Simples.NotificationStack.Stack.NotificationStack)notificationStackField.GetValue(fns);

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
        {            var topics = GetAllTopics(regionEndpoint, topicName);
            
            topics.ForEach(t => DeleteTopic(regionEndpoint, t));

            Topic topic;
            if (TryGetTopic(regionEndpoint, topicName, out topic))
            {
                throw new Exception("Deleted topic still exists!");
            }
        }

        public static void DeleteTopic(RegionEndpoint regionEndpoint, Topic topic)
        {
            var client = AWSClientFactory.CreateAmazonSNSClient(regionEndpoint);
            client.DeleteTopic(new DeleteTopicRequest { TopicArn = topic.TopicArn });
        }

        private static List<Topic> GetAllTopics(RegionEndpoint regionEndpoint, string topicName)
        {
            var client = AWSClientFactory.CreateAmazonSNSClient(regionEndpoint);
            var topics = client.ListTopics(new ListTopicsRequest());
            return topics.ListTopicsResult.Topics;
        }

        public static bool TryGetTopic(RegionEndpoint regionEndpoint, string topicName, out Topic topic)
        {
            var client = AWSClientFactory.CreateAmazonSNSClient(regionEndpoint);
            var topics = client.ListTopics(new ListTopicsRequest());

            topic = topics.ListTopicsResult.Topics.SingleOrDefault(x => x.TopicArn.IndexOf(topicName, StringComparison.InvariantCultureIgnoreCase) >= 0);

            return topic != null;
        }
    }
}