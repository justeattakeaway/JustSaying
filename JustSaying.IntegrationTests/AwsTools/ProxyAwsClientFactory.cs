using System.Collections.Generic;
using System.Threading;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools;
using NSubstitute;

namespace JustSaying.IntegrationTests.AwsTools
{
    /// <summary>
    /// An AWS Client Factory which forwards all AWS calls to SNS/SQS clients and stores all calls in a dictionary.
    /// Used to inspect what operations and which arguments have been passed
    /// </summary>
    internal class ProxyAwsClientFactory : IAwsClientFactory
    {
        public Dictionary<string, Dictionary<string, List<object>>> Counters { get; } = new Dictionary<string, Dictionary<string, List<object>>>();

        public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
        {
            var innerClient = CreateMeABus.DefaultClientFactory().GetSnsClient(region);
            var client = Substitute.For<IAmazonSimpleNotificationService>();

            client.CreateTopicAsync(Arg.Any<CreateTopicRequest>())
                  .ReturnsForAnyArgs(r => innerClient.CreateTopicAsync(r.Arg<CreateTopicRequest>(), r.Arg<CancellationToken>()))
                  .AndDoes(r => Increment("CreateTopic", r.Arg<CreateTopicRequest>().Name, r.Arg<CreateTopicRequest>()));

            client.FindTopicAsync(Arg.Any<string>())
                  .ReturnsForAnyArgs(r => innerClient.FindTopicAsync(r.Arg<string>()))
                  .AndDoes(r => Increment("FindTopic", r.Arg<string>(), r.Arg<string>()));

            client.GetTopicAttributesAsync(Arg.Any<string>())
                  .ReturnsForAnyArgs(r => innerClient.GetTopicAttributesAsync(r.Arg<string>(), r.Arg<CancellationToken>()))
                  .AndDoes(r => Increment("GetTopicAttributes", r.Arg<string>(), r.Arg<string>()));

            return client;
        }

        private void Increment(string operationName, string paramKey, params object[] extraParams)
        {
            if (!Counters.ContainsKey(operationName))
            {
                Counters[operationName] = new Dictionary<string, List<object>>();
            }

            var operation = Counters[operationName];

            if (!operation.TryGetValue(paramKey, out List<object> paramOperation))
            {
                operation[paramKey] = paramOperation = new List<object>();
            }

            paramOperation.Add(extraParams);
        }

        public IAmazonSQS GetSqsClient(RegionEndpoint region)
        {
            var innerClient = CreateMeABus.DefaultClientFactory().GetSqsClient(region);
            var client = Substitute.For<IAmazonSQS>();

            client.ListQueuesAsync(Arg.Any<ListQueuesRequest>())
                .ReturnsForAnyArgs(r => innerClient.ListQueuesAsync(r.Arg<ListQueuesRequest>(), r.Arg<CancellationToken>()))
                .AndDoes(r => Increment("ListQueues", r.Arg<ListQueuesRequest>().QueueNamePrefix, r.Arg<ListQueuesRequest>()));

            client.CreateQueueAsync(Arg.Any<CreateQueueRequest>())
                .ReturnsForAnyArgs(r => innerClient.CreateQueueAsync(r.Arg<CreateQueueRequest>(), r.Arg<CancellationToken>()))
                .AndDoes(r => Increment("CreateQueue", r.Arg<CreateQueueRequest>().QueueName, r.Arg<CreateQueueRequest>()));

            client.CreateQueueAsync(Arg.Any<string>())
                .ReturnsForAnyArgs(r => innerClient.CreateQueueAsync(r.Arg<string>(), r.Arg<CancellationToken>()))
                .AndDoes(r => Increment("CreateQueue", r.Arg<string>()));

            client.GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>())
                .ReturnsForAnyArgs(r => innerClient.GetQueueAttributesAsync(r.Arg<GetQueueAttributesRequest>(), r.Arg<CancellationToken>()))
                .AndDoes(r => Increment("GetQueueAttributes", r.Arg<GetQueueAttributesRequest>().QueueUrl, r.Arg<GetQueueAttributesRequest>()));

            client.GetQueueAttributesAsync(Arg.Any<string>(), Arg.Any<List<string>>())
                .ReturnsForAnyArgs(r => innerClient.GetQueueAttributesAsync(r.Arg<string>(), r.Arg<List<string>>(), r.Arg<CancellationToken>()))
                .AndDoes(r => Increment("GetQueueAttributes", r.Arg<string>(), r.Arg<List<string>>()));

            client.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>())
                .ReturnsForAnyArgs(r => innerClient.ReceiveMessageAsync(r.Arg<ReceiveMessageRequest>(), r.Arg<CancellationToken>()))
                .AndDoes(r => Increment("ReceiveMessageAsync", r.Arg<ReceiveMessageRequest>().QueueUrl, r.Arg<ReceiveMessageRequest>()));

            return client;
        }
    }
}
