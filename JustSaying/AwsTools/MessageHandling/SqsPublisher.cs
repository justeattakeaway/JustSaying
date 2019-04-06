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
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SqsPublisher : SqsQueueByName, IMessagePublisher
    {
        private readonly IAmazonSQS _client;
        private readonly IMessageSerializationRegister _serializationRegister;
        public Action<MessageResponse, Message> MessageResponseLogger { get; set; }

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

        public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
        {
            var request = BuildSendMessageRequest(message, metadata);
            try
            {
                SendMessageResponse response = await _client.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);

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
            catch (AmazonServiceException ex)
            {
                throw new PublishException(
                    $"Failed to publish message to SQS. {nameof(request.QueueUrl)}: {request.QueueUrl},{nameof(request.MessageBody)}: {request.MessageBody}",
                    ex);
            }
        }

        private SendMessageRequest BuildSendMessageRequest(Message message, PublishMetadata metadata)
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

        public string GetMessageInContext(Message message) => _serializationRegister.Serialize(message, serializeForSnsPublishing: false);
    }
}
