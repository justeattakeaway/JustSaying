using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SqsPublisher : SqsQueueByName, IMessagePublisher
    {
        private readonly IAmazonSQS _client;
        private readonly IMessageSerialisationRegister _serialisationRegister;
        private readonly IMessageResponseLogger _messageResponseLogger;

        public SqsPublisher(RegionEndpoint region, string queueName, IAmazonSQS client,
            int retryCountBeforeSendingToErrorQueue, IMessageSerialisationRegister serialisationRegister,
            IMessageResponseLogger messageResponseLogger, ILoggerFactory loggerFactory)
            : base(region, queueName, client, retryCountBeforeSendingToErrorQueue, loggerFactory)
        {
            _client = client;
            _serialisationRegister = serialisationRegister;
            _messageResponseLogger = messageResponseLogger;
        }

#if AWS_SDK_HAS_SYNC
        public void Publish(Message message)
        {
            var request = BuildSendMessageRequest(message);

            try
            {
                var response = _client.SendMessage(request);

                _messageResponseLogger?.ResponseLogger?.Invoke(new MessageResponse
                {
                    HttpStatusCode = response?.HttpStatusCode,
                    MessageId = response?.MessageId
                }, message);
            }
            catch (Exception ex)
            {
                throw new PublishException(
                    $"Failed to publish message to SQS. QueueUrl: {request.QueueUrl} MessageBody: {request.MessageBody}",
                    ex);
            }
        }
#endif

        public Task PublishAsync(Message message) => PublishAsync(message, CancellationToken.None);

        public async Task PublishAsync(Message message, CancellationToken cancellationToken)
        {
            var request = BuildSendMessageRequest(message);
            try
            {
                var response = await _client.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);

                if (_messageResponseLogger?.ResponseLoggerAsync != null)
                {
                    await _messageResponseLogger.ResponseLoggerAsync(new MessageResponse
                    {
                        HttpStatusCode = response?.HttpStatusCode,
                        MessageId = response?.MessageId
                    }, message);
                } else
                {
                    _messageResponseLogger?.ResponseLogger?.Invoke(new MessageResponse
                    {
                        HttpStatusCode = response?.HttpStatusCode,
                        MessageId = response?.MessageId
                    }, message);
                }
            }
            catch (Exception ex)
            {
                throw new PublishException(
                    $"Failed to publish message to SQS. QueueUrl: {request.QueueUrl} MessageBody: {request.MessageBody}",
                    ex);
            }
        }

        private SendMessageRequest BuildSendMessageRequest(Message message)
        {
            var request = new SendMessageRequest
            {
                MessageBody = GetMessageInContext(message),
                QueueUrl = Url
            };

            if (message.DelaySeconds.HasValue)
            {
                request.DelaySeconds = message.DelaySeconds.Value;
            }
            return request;
        }

        public string GetMessageInContext(Message message) => _serialisationRegister.Serialise(message, serializeForSnsPublishing: false);
    }
}
