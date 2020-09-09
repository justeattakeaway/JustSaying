using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests
{
    public sealed class FakeAmazonSqs : IAmazonSQS
    {
        private readonly Func<IEnumerable<ReceiveMessageResponse>> _getMessages;

        public IList<DeleteMessageRequest> DeleteMessageRequests { get; }
        public IList<ReceiveMessageRequest> ReceiveMessageRequests { get; }


        public FakeAmazonSqs(Func<IEnumerable<ReceiveMessageResponse>> getMessages)
        {
            _getMessages = getMessages;
            DeleteMessageRequests = new List<DeleteMessageRequest>();
            ReceiveMessageRequests = new List<ReceiveMessageRequest>();
        }

        public void Dispose()
        { }

        public Task<Dictionary<string, string>> GetAttributesAsync(string queueUrl)
        {
            return Task.FromResult(new Dictionary<string, string>());
        }

        public Task SetAttributesAsync(string queueUrl, Dictionary<string, string> attributes)
        {
            return Task.CompletedTask;
        }

        public IClientConfig Config { get; }

        public ISQSPaginatorFactory Paginators { get; }

        public Task<string> AuthorizeS3ToSendMessageAsync(string queueUrl, string bucket)
        {
            return Task.FromResult("");
        }

        public Task<AddPermissionResponse> AddPermissionAsync(
            string queueUrl,
            string label,
            List<string> awsAccountIds,
            List<string> actions,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new AddPermissionResponse());
        }

        public Task<AddPermissionResponse> AddPermissionAsync(
            AddPermissionRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new AddPermissionResponse());
        }

        public Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(
            string queueUrl,
            string receiptHandle,
            int visibilityTimeout,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new ChangeMessageVisibilityResponse());
        }

        public Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(
            ChangeMessageVisibilityRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new ChangeMessageVisibilityResponse());
        }

        public Task<ChangeMessageVisibilityBatchResponse> ChangeMessageVisibilityBatchAsync(
            string queueUrl,
            List<ChangeMessageVisibilityBatchRequestEntry> entries,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new ChangeMessageVisibilityBatchResponse());
        }

        public Task<ChangeMessageVisibilityBatchResponse> ChangeMessageVisibilityBatchAsync(
            ChangeMessageVisibilityBatchRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new ChangeMessageVisibilityBatchResponse());
        }

        public Task<CreateQueueResponse> CreateQueueAsync(
            string queueName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new CreateQueueResponse());
        }

        public Task<CreateQueueResponse> CreateQueueAsync(
            CreateQueueRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new CreateQueueResponse());
        }

        public Task<DeleteMessageResponse> DeleteMessageAsync(
            string queueUrl,
            string receiptHandle,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return DeleteMessageAsync(new DeleteMessageRequest(queueUrl, receiptHandle), cancellationToken);
        }

        public Task<DeleteMessageResponse> DeleteMessageAsync(
            DeleteMessageRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            DeleteMessageRequests.Add(request);
            return Task.FromResult(new DeleteMessageResponse());
        }


        public Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(
            string queueUrl,
            List<DeleteMessageBatchRequestEntry> entries,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new DeleteMessageBatchResponse());
        }

        public Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(
            DeleteMessageBatchRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new DeleteMessageBatchResponse());
        }

        public Task<DeleteQueueResponse> DeleteQueueAsync(
            string queueUrl,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new DeleteQueueResponse());
        }

        public Task<DeleteQueueResponse> DeleteQueueAsync(
            DeleteQueueRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new DeleteQueueResponse());
        }

        public Task<GetQueueAttributesResponse> GetQueueAttributesAsync(
            string queueUrl,
            List<string> attributeNames,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new GetQueueAttributesResponse());
        }

        public Task<GetQueueAttributesResponse> GetQueueAttributesAsync(
            GetQueueAttributesRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new GetQueueAttributesResponse());
        }

        public Task<GetQueueUrlResponse> GetQueueUrlAsync(
            string queueName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return GetQueueUrlAsync(new GetQueueUrlRequest(queueName), cancellationToken);
        }

        public Task<GetQueueUrlResponse> GetQueueUrlAsync(
            GetQueueUrlRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new GetQueueUrlResponse
            {
                QueueUrl = $"https://testqueues.com/{request.QueueName}"
            });
        }

        public Task<ListDeadLetterSourceQueuesResponse> ListDeadLetterSourceQueuesAsync(
            ListDeadLetterSourceQueuesRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new ListDeadLetterSourceQueuesResponse());
        }

        public Task<ListQueuesResponse> ListQueuesAsync(
            string queueNamePrefix,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new ListQueuesResponse());
        }

        public Task<ListQueuesResponse> ListQueuesAsync(
            ListQueuesRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new ListQueuesResponse());
        }

        public Task<ListQueueTagsResponse> ListQueueTagsAsync(
            ListQueueTagsRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new ListQueueTagsResponse());
        }

        public Task<PurgeQueueResponse> PurgeQueueAsync(
            string queueUrl,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new PurgeQueueResponse());
        }

        public Task<PurgeQueueResponse> PurgeQueueAsync(
            PurgeQueueRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new PurgeQueueResponse());
        }

        public Task<ReceiveMessageResponse> ReceiveMessageAsync(
            string queueUrl,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return ReceiveMessageAsync(new ReceiveMessageRequest(queueUrl), cancellationToken);
        }


        private IEnumerator<ReceiveMessageResponse> _getMessagesEnumerator;
        public async Task<ReceiveMessageResponse> ReceiveMessageAsync(
            ReceiveMessageRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            await Task.Delay(50, cancellationToken);

            ReceiveMessageRequests.Add(request);

            _getMessagesEnumerator ??= _getMessages().GetEnumerator();
            _getMessagesEnumerator.MoveNext();

            return new ReceiveMessageResponse()
            {
                Messages = _getMessagesEnumerator.Current.Messages
            };
        }

        public Task<RemovePermissionResponse> RemovePermissionAsync(
            string queueUrl,
            string label,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new RemovePermissionResponse());
        }

        public Task<RemovePermissionResponse> RemovePermissionAsync(
            RemovePermissionRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new RemovePermissionResponse());
        }

        public Task<SendMessageResponse> SendMessageAsync(
            string queueUrl,
            string messageBody,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new SendMessageResponse());
        }

        public Task<SendMessageResponse> SendMessageAsync(
            SendMessageRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new SendMessageResponse());
        }

        public Task<SendMessageBatchResponse> SendMessageBatchAsync(
            string queueUrl,
            List<SendMessageBatchRequestEntry> entries,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new SendMessageBatchResponse());
        }

        public Task<SendMessageBatchResponse> SendMessageBatchAsync(
            SendMessageBatchRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new SendMessageBatchResponse());
        }

        public Task<SetQueueAttributesResponse> SetQueueAttributesAsync(
            string queueUrl,
            Dictionary<string, string> attributes,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new SetQueueAttributesResponse());
        }

        public Task<SetQueueAttributesResponse> SetQueueAttributesAsync(
            SetQueueAttributesRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new SetQueueAttributesResponse());
        }

        public Task<TagQueueResponse> TagQueueAsync(
            TagQueueRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new TagQueueResponse());
        }

        public Task<UntagQueueResponse> UntagQueueAsync(
            UntagQueueRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(new UntagQueueResponse());
        }
    }
}
