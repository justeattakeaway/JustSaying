using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling
{
    public sealed class SqsMessagePublisher : IMessagePublisher
    {
        private readonly IAmazonSQS _client;
        private readonly IMessageSerializationRegister _serializationRegister;
        private readonly ILogger _logger;
        public Action<MessageResponse, Message> MessageResponseLogger { get; set; }

        public Uri QueueUrl { get; set; }

        public SqsMessagePublisher(
            IAmazonSQS client,
            IMessageSerializationRegister serializationRegister,
            ILoggerFactory loggerFactory)
        {
            _client = client;
            _serializationRegister = serializationRegister;
            _logger = loggerFactory.CreateLogger("JustSaying.Publish");
        }

        // TODO: This type shouldn't be an IMessagePublisher
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task PublishAsync(Message message, CancellationToken cancellationToken)
            => await PublishAsync(message, null, cancellationToken).ConfigureAwait(false);

        public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
        {
            if (QueueUrl is null) throw new PublishException("Queue URL was null, perhaps you have not called `StartAsync` on the `IMessagePublisher` before use.");

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

            using (_logger.BeginScope(new[]
            {
                new KeyValuePair<string, object>("AwsRequestId", response?.MessageId)
            }))
            {
                _logger.LogInformation(
                    "Published message {MessageId} of type {MessageType} to {DestinationType} '{MessageDestination}'.",
                    message.Id,
                    message.GetType().Name,
                    "Queue",
                    request.QueueUrl);
            }

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

        private SendMessageRequest BuildSendMessageRequest(Message message, PublishMetadata metadata)
        {
            var request = new SendMessageRequest
            {
                MessageBody = GetMessageInContext(message),
                QueueUrl = QueueUrl.AbsoluteUri,
            };

            if (metadata?.Delay != null)
            {
                request.DelaySeconds = (int)metadata.Delay.Value.TotalSeconds;
            }
            return request;
        }

        public string GetMessageInContext(Message message) => _serializationRegister.Serialize(message, serializeForSnsPublishing: false);

        public InterrogationResult Interrogate()
        {
            return InterrogationResult.Empty;
        }
    }
}
