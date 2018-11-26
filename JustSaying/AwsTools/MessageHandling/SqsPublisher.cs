using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
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

        public async Task PublishAsync(PublishEnvelope envelope, CancellationToken cancellationToken)
        {
            var request = BuildSendMessageRequest(envelope);
            try
            {
                var response = await _client.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
                MessageResponseLogger?.Invoke(new MessageResponse
                {
                    HttpStatusCode = response?.HttpStatusCode,
                    MessageId = response?.MessageId
                }, envelope.Message);
            }
            catch (Exception ex)
            {
                throw new PublishException(
                    $"Failed to publish message to SQS. {nameof(request.QueueUrl)}: {request.QueueUrl},{nameof(request.MessageBody)}: {request.MessageBody}",
                    ex);
            }
        }

        private SendMessageRequest BuildSendMessageRequest(PublishEnvelope envelope)
        {
            var request = new SendMessageRequest
            {
                MessageBody = GetMessageInContext(envelope.Message),
                QueueUrl = Uri?.AbsoluteUri
            };

            if (envelope.Delay.HasValue)
            {
                request.DelaySeconds = (int)envelope.Delay.Value.TotalSeconds;
            }
            return request;
        }

        public string GetMessageInContext(Message message) => _serializationRegister.Serialize(message, serializeForSnsPublishing: false);
    }
}
