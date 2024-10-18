using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Endpoints;
using Amazon.Runtime.SharedInterfaces;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools;

namespace JustSaying.TestingFramework;

public sealed class InspectableSqsClient(IAmazonSQS innerAmazonSqsClient) : IAmazonSQS
{
    public List<ReceiveMessageResponse> ReceiveMessageResponses { get; } = [];

    async Task<ReceiveMessageResponse> IAmazonSQS.ReceiveMessageAsync(ReceiveMessageRequest request, CancellationToken cancellationToken)
    {
        var response = await innerAmazonSqsClient.ReceiveMessageAsync(request, cancellationToken);
        ReceiveMessageResponses.Add(response);
        return response;
    }

    // Proxied implementations
    void IDisposable.Dispose() => innerAmazonSqsClient.Dispose();
    Task<Dictionary<string, string>> ICoreAmazonSQS.GetAttributesAsync(string queueUrl) => innerAmazonSqsClient.GetAttributesAsync(queueUrl);
    Task ICoreAmazonSQS.SetAttributesAsync(string queueUrl, Dictionary<string, string> attributes) => innerAmazonSqsClient.SetAttributesAsync(queueUrl, attributes);
    IClientConfig IAmazonService.Config => innerAmazonSqsClient.Config;
    Task<string> IAmazonSQS.AuthorizeS3ToSendMessageAsync(string queueUrl, string bucket) => innerAmazonSqsClient.AuthorizeS3ToSendMessageAsync(queueUrl, bucket);
    Task<AddPermissionResponse> IAmazonSQS.AddPermissionAsync(string queueUrl, string label, List<string> awsAccountIds, List<string> actions, CancellationToken cancellationToken) => innerAmazonSqsClient.AddPermissionAsync(queueUrl, label, awsAccountIds, actions, cancellationToken);
    Task<AddPermissionResponse> IAmazonSQS.AddPermissionAsync(AddPermissionRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.AddPermissionAsync(request, cancellationToken);
    Task<CancelMessageMoveTaskResponse> IAmazonSQS.CancelMessageMoveTaskAsync(CancelMessageMoveTaskRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.CancelMessageMoveTaskAsync(request, cancellationToken);
    Task<ChangeMessageVisibilityResponse> IAmazonSQS.ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int visibilityTimeout, CancellationToken cancellationToken) => innerAmazonSqsClient.ChangeMessageVisibilityAsync(queueUrl, receiptHandle, visibilityTimeout, cancellationToken);
    Task<ChangeMessageVisibilityResponse> IAmazonSQS.ChangeMessageVisibilityAsync(ChangeMessageVisibilityRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.ChangeMessageVisibilityAsync(request, cancellationToken);
    Task<ChangeMessageVisibilityBatchResponse> IAmazonSQS.ChangeMessageVisibilityBatchAsync(string queueUrl, List<ChangeMessageVisibilityBatchRequestEntry> entries, CancellationToken cancellationToken) => innerAmazonSqsClient.ChangeMessageVisibilityBatchAsync(queueUrl, entries, cancellationToken);
    Task<ChangeMessageVisibilityBatchResponse> IAmazonSQS.ChangeMessageVisibilityBatchAsync(ChangeMessageVisibilityBatchRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.ChangeMessageVisibilityBatchAsync(request, cancellationToken);
    Task<CreateQueueResponse> IAmazonSQS.CreateQueueAsync(string queueName, CancellationToken cancellationToken) => innerAmazonSqsClient.CreateQueueAsync(queueName, cancellationToken);
    Task<CreateQueueResponse> IAmazonSQS.CreateQueueAsync(CreateQueueRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.CreateQueueAsync(request, cancellationToken);
    Task<DeleteMessageResponse> IAmazonSQS.DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken cancellationToken) => innerAmazonSqsClient.DeleteMessageAsync(queueUrl, receiptHandle, cancellationToken);
    Task<DeleteMessageResponse> IAmazonSQS.DeleteMessageAsync(DeleteMessageRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.DeleteMessageAsync(request, cancellationToken);
    Task<DeleteMessageBatchResponse> IAmazonSQS.DeleteMessageBatchAsync(string queueUrl, List<DeleteMessageBatchRequestEntry> entries, CancellationToken cancellationToken) => innerAmazonSqsClient.DeleteMessageBatchAsync(queueUrl, entries, cancellationToken);
    Task<DeleteMessageBatchResponse> IAmazonSQS.DeleteMessageBatchAsync(DeleteMessageBatchRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.DeleteMessageBatchAsync(request, cancellationToken);
    Task<DeleteQueueResponse> IAmazonSQS.DeleteQueueAsync(string queueUrl, CancellationToken cancellationToken) => innerAmazonSqsClient.DeleteQueueAsync(queueUrl, cancellationToken);
    Task<DeleteQueueResponse> IAmazonSQS.DeleteQueueAsync(DeleteQueueRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.DeleteQueueAsync(request, cancellationToken);
    Task<GetQueueAttributesResponse> IAmazonSQS.GetQueueAttributesAsync(string queueUrl, List<string> attributeNames, CancellationToken cancellationToken) => innerAmazonSqsClient.GetQueueAttributesAsync(queueUrl, attributeNames, cancellationToken);
    Task<GetQueueAttributesResponse> IAmazonSQS.GetQueueAttributesAsync(GetQueueAttributesRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.GetQueueAttributesAsync(request, cancellationToken);
    Task<GetQueueUrlResponse> IAmazonSQS.GetQueueUrlAsync(string queueName, CancellationToken cancellationToken) => innerAmazonSqsClient.GetQueueUrlAsync(queueName, cancellationToken);
    Task<GetQueueUrlResponse> IAmazonSQS.GetQueueUrlAsync(GetQueueUrlRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.GetQueueUrlAsync(request, cancellationToken);
    Task<ListDeadLetterSourceQueuesResponse> IAmazonSQS.ListDeadLetterSourceQueuesAsync(ListDeadLetterSourceQueuesRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.ListDeadLetterSourceQueuesAsync(request, cancellationToken);
    Task<ListMessageMoveTasksResponse> IAmazonSQS.ListMessageMoveTasksAsync(ListMessageMoveTasksRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.ListMessageMoveTasksAsync(request, cancellationToken);
    Task<ListQueuesResponse> IAmazonSQS.ListQueuesAsync(string queueNamePrefix, CancellationToken cancellationToken) => innerAmazonSqsClient.ListQueuesAsync(queueNamePrefix, cancellationToken);
    Task<ListQueuesResponse> IAmazonSQS.ListQueuesAsync(ListQueuesRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.ListQueuesAsync(request, cancellationToken);
    Task<ListQueueTagsResponse> IAmazonSQS.ListQueueTagsAsync(ListQueueTagsRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.ListQueueTagsAsync(request, cancellationToken);
    Task<PurgeQueueResponse> IAmazonSQS.PurgeQueueAsync(string queueUrl, CancellationToken cancellationToken) => innerAmazonSqsClient.PurgeQueueAsync(queueUrl, cancellationToken);
    Task<PurgeQueueResponse> IAmazonSQS.PurgeQueueAsync(PurgeQueueRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.PurgeQueueAsync(request, cancellationToken);
    Task<ReceiveMessageResponse> IAmazonSQS.ReceiveMessageAsync(string queueUrl, CancellationToken cancellationToken) => innerAmazonSqsClient.ReceiveMessageAsync(queueUrl, cancellationToken);
    Task<RemovePermissionResponse> IAmazonSQS.RemovePermissionAsync(string queueUrl, string label, CancellationToken cancellationToken) => innerAmazonSqsClient.RemovePermissionAsync(queueUrl, label, cancellationToken);
    Task<RemovePermissionResponse> IAmazonSQS.RemovePermissionAsync(RemovePermissionRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.RemovePermissionAsync(request, cancellationToken);
    Task<SendMessageResponse> IAmazonSQS.SendMessageAsync(string queueUrl, string messageBody, CancellationToken cancellationToken) => innerAmazonSqsClient.SendMessageAsync(queueUrl, messageBody, cancellationToken);
    Task<SendMessageResponse> IAmazonSQS.SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.SendMessageAsync(request, cancellationToken);
    Task<SendMessageBatchResponse> IAmazonSQS.SendMessageBatchAsync(string queueUrl, List<SendMessageBatchRequestEntry> entries, CancellationToken cancellationToken) => innerAmazonSqsClient.SendMessageBatchAsync(queueUrl, entries, cancellationToken);
    Task<SendMessageBatchResponse> IAmazonSQS.SendMessageBatchAsync(SendMessageBatchRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.SendMessageBatchAsync(request, cancellationToken);
    Task<SetQueueAttributesResponse> IAmazonSQS.SetQueueAttributesAsync(string queueUrl, Dictionary<string, string> attributes, CancellationToken cancellationToken) => innerAmazonSqsClient.SetQueueAttributesAsync(queueUrl, attributes, cancellationToken);
    Task<SetQueueAttributesResponse> IAmazonSQS.SetQueueAttributesAsync(SetQueueAttributesRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.SetQueueAttributesAsync(request, cancellationToken);
    Task<StartMessageMoveTaskResponse> IAmazonSQS.StartMessageMoveTaskAsync(StartMessageMoveTaskRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.StartMessageMoveTaskAsync(request, cancellationToken);
    Task<TagQueueResponse> IAmazonSQS.TagQueueAsync(TagQueueRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.TagQueueAsync(request, cancellationToken);
    Task<UntagQueueResponse> IAmazonSQS.UntagQueueAsync(UntagQueueRequest request, CancellationToken cancellationToken) => innerAmazonSqsClient.UntagQueueAsync(request, cancellationToken);
    Endpoint IAmazonSQS.DetermineServiceOperationEndpoint(AmazonWebServiceRequest request) => innerAmazonSqsClient.DetermineServiceOperationEndpoint(request);
    ISQSPaginatorFactory IAmazonSQS.Paginators => innerAmazonSqsClient.Paginators;
}

public sealed class InspectableClientFactory(IAwsClientFactory innerClientFactory) : IAwsClientFactory
{
    public InspectableSqsClient InspectableSqsClient { get; } = new(innerClientFactory.GetSqsClient(TestEnvironment.Region));

    public IAmazonSimpleNotificationService GetSnsClient(RegionEndpoint region)
    {
        return innerClientFactory.GetSnsClient(region);
    }

    public IAmazonSQS GetSqsClient(RegionEndpoint region)
    {
        return InspectableSqsClient;
    }
}
