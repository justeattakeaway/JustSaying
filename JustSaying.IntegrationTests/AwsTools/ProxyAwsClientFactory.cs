using System.Collections.Generic;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using NSubstitute;

namespace JustSaying.AwsTools.IntegrationTests
{
    /// <summary>
    /// An AWS Client Factory which forwards all AWS calls to SNS/SQS clients
    /// and stores all calls in a dictionary.
    ///
    /// Use to inspect what operations and which arguments have been passed
    /// </summary>
    public class ProxyAwsClientFactory : IAwsClientFactory
    {
        /// <summary>
        /// Root dictionary key is aws operation, such as ListTopics, GetQueue, etc.
        ///
        /// Each inner dictionary contains a grouped collection of calls to that operator. Key for the grouping
        /// is chosen on a per operation basis, but in many cases this is the queue or topic name.
        ///
        /// List of objects is the full list of arguments when that call was made.
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, List<object>>> _counters
            = new Dictionary<string, Dictionary<string, List<object>>>();

        public Dictionary<string, Dictionary<string, List<object>>> Counters => _counters;

        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        {
            var innerClient = CreateMeABus.DefaultClientFactory().GetSnsClient(region);
            var client = Substitute.For<IAmazonSimpleNotificationService>();

            client.CreateTopicAsync(Arg.Any<CreateTopicRequest>())
                .ReturnsForAnyArgs(r => innerClient.CreateTopicAsync(r.Arg<CreateTopicRequest>()))
                .AndDoes(r => Increment("CreateTopic", r.Arg<CreateTopicRequest>().Name, r.Arg<CreateTopicRequest>()));

            client.FindTopicAsync(Arg.Any<string>())
                .ReturnsForAnyArgs(r => innerClient.FindTopicAsync(r.Arg<string>()))
                .AndDoes(r => Increment("FindTopic", r.Arg<string>(), r.Arg<string>()));

            client.GetTopicAttributesAsync(Arg.Any<string>())
            .ReturnsForAnyArgs(r => innerClient.GetTopicAttributesAsync(r.Arg<string>()))
            .AndDoes(r => Increment("GetTopicAttributes", r.Arg<string>(), r.Arg<string>()));

            return client;
        }

        private void Increment(string operationName, string paramKey, params object[] extraParams)
        {
            if (!_counters.ContainsKey(operationName))
            {
                _counters[operationName] = new Dictionary<string, List<object>>();
            }

            var operation = _counters[operationName];
            if (!operation.ContainsKey(paramKey))
            {
                operation.Add(paramKey, new List<object>());
            }

            var paramOperation = operation[paramKey];
            paramOperation.Add(extraParams);
        }

        public IAmazonSQS GetSqsClient(RegionEndpoint region)
        {
            var innerClient = CreateMeABus.DefaultClientFactory().GetSqsClient(region);
            var client = Substitute.For<IAmazonSQS>();

            client.ListQueuesAsync(Arg.Any<ListQueuesRequest>())
                .ReturnsForAnyArgs(r => innerClient.ListQueuesAsync(r.Arg<ListQueuesRequest>()))
                .AndDoes(r => Increment("ListQueues", r.Arg<ListQueuesRequest>().QueueNamePrefix, r.Arg<ListQueuesRequest>()));

            client.CreateQueueAsync(Arg.Any<CreateQueueRequest>())
                .ReturnsForAnyArgs(r => innerClient.CreateQueueAsync(r.Arg<CreateQueueRequest>()))
                .AndDoes(r => Increment("CreateQueue", r.Arg<CreateQueueRequest>().QueueName, r.Arg<CreateQueueRequest>()));

            client.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
                .ReturnsForAnyArgs(r => innerClient.GetQueueAttributesAsync(r.Arg<GetQueueAttributesRequest>()))
                .AndDoes(r => Increment("GetQueueAttributes", r.Arg<GetQueueAttributesRequest>().QueueUrl, r.Arg<GetQueueAttributesRequest>()));

            client.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>())
                .ReturnsForAnyArgs(r => innerClient.ReceiveMessageAsync(r.Arg<ReceiveMessageRequest>()))
                .AndDoes(r => Increment("ReceiveMessageAsync", r.Arg<ReceiveMessageRequest>().QueueUrl, r.Arg<ReceiveMessageRequest>()));

            return client;
        }
    }
}
