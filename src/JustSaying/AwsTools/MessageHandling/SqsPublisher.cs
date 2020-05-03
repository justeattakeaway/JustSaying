using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SqsPublisher : SqsQueueByName, IMessagePublisher
    {
        private readonly IAmazonSQS _client;
        private readonly IMessageSerializationRegister _serializationRegister;
        public Action<MessageResponse, object> MessageResponseLogger { get; set; }

        public SqsPublisher(
            RegionEndpoint region,
            string queueName,
            IAmazonSQS client,
            int retryCountBeforeSendingToErrorQueue,
            IMessageSerializationRegister serializationRegister,
            ILoggerFactory loggerFactory)
            : base(region, queueName, client, retryCountBeforeSendingToErrorQueue, loggerFactory)
        {
            _client = client;
            _serializationRegister = serializationRegister;
        }

        public async Task PublishAsync<T>(T message, PublishMetadata metadata, CancellationToken cancellationToken)
            where T : class
        {
            var request = BuildSendMessageRequest(message, metadata);
            SendMessageResponse response;
            try
            {
                response = await _client.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonServiceException ex)
            {
                throw new PublishException(
                    $"Failed to publish message to SQS. {nameof(request.QueueUrl)}: {request.QueueUrl},{nameof(request.MessageBody)}: {request.MessageBody}",
                    ex);
            }

            Logger.LogInformation(
                "Published message to queue '{QueueUrl}' with content '{MessageBody}'.",
                request.QueueUrl,
                request.MessageBody);

            if (MessageResponseLogger != null)
            {
                var responseData = new MessageResponse
                {
                    HttpStatusCode = response?.HttpStatusCode,
                    MessageId = response?.MessageId,
                    ResponseMetadata = response?.ResponseMetadata
                };
                MessageResponseLogger.Invoke(responseData, message);
            }
        }

        private SendMessageRequest BuildSendMessageRequest<T>(T message, PublishMetadata metadata)
            where T : class
        {
            var request = new SendMessageRequest
            {
                MessageBody = GetMessageInContext(message),
                QueueUrl = Uri?.AbsoluteUri
            };

            if (metadata?.Delay != null)
            {
                request.DelaySeconds = (int)metadata.Delay.Value.TotalSeconds;
            }
            return request;
        }

        public string GetMessageInContext<T>(T message) where T : class => _serializationRegister.Serialize(message, serializeForSnsPublishing: false);
    }
}
