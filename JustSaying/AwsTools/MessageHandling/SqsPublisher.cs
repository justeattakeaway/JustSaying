using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SqsPublisher : SqsQueueByName, IMessagePublisher
    {
        private readonly IAmazonSQS _client;
        private readonly IMessageSerialisationRegister _serialisationRegister;
        public Action<MessageResponse, Message> MessageResponseLogger { get; set; }

        public SqsPublisher(RegionEndpoint region, string queueName, IAmazonSQS client,
            int retryCountBeforeSendingToErrorQueue, IMessageSerialisationRegister serialisationRegister,
            ILoggerFactory loggerFactory)
            : base(region, queueName, client, retryCountBeforeSendingToErrorQueue, loggerFactory)
        {
            _client = client;
            _serialisationRegister = serialisationRegister;
        }

        public async Task PublishAsync(PublishEnvelope env, CancellationToken cancellationToken)
        {
            var request = BuildSendMessageRequest(env);
            try
            {
                var response = await _client.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
                MessageResponseLogger?.Invoke(new MessageResponse
                {
                    HttpStatusCode = response?.HttpStatusCode,
                    MessageId = response?.MessageId
                }, env.Message);
            }
            catch (Exception ex)
            {
                throw new PublishException(
                    $"Failed to publish message to SQS. {nameof(request.QueueUrl)}: {request.QueueUrl},{nameof(request.MessageBody)}: {request.MessageBody}",
                    ex);
            }
        }

        private SendMessageRequest BuildSendMessageRequest(PublishEnvelope env)
        {
            var request = new SendMessageRequest
            {
                MessageBody = GetMessageInContext(env.Message),
                QueueUrl = Uri?.AbsoluteUri
            };

            if (env.DelaySeconds.HasValue)
            {
                request.DelaySeconds = env.DelaySeconds.Value;
            }
            return request;
        }

        public string GetMessageInContext(Message message) => _serialisationRegister.Serialise(message, serializeForSnsPublishing: false);
    }
}
