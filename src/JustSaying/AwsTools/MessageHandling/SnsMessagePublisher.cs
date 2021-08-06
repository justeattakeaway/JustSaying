using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;
using MessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SnsMessagePublisher : IMessagePublisher, IInterrogable
    {
        private readonly IMessageSerializationRegister _serializationRegister;
        private readonly IMessageSubjectProvider _messageSubjectProvider;
        private readonly Func<Exception, Message, bool> _handleException;
        public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
        public string Arn { get; internal set; }
        protected IAmazonSimpleNotificationService Client { get; }
        private readonly ILogger _logger;

        public SnsMessagePublisher(
            string topicArn,
            IAmazonSimpleNotificationService client,
            IMessageSerializationRegister serializationRegister,
            ILoggerFactory loggerFactory,
            IMessageSubjectProvider messageSubjectProvider,
            Func<Exception, Message, bool> handleException = null)
            : this(client, serializationRegister, loggerFactory, messageSubjectProvider, handleException)
        {
            Arn = topicArn;
        }

        public SnsMessagePublisher(
            IAmazonSimpleNotificationService client,
            IMessageSerializationRegister serializationRegister,
            ILoggerFactory loggerFactory,
            IMessageSubjectProvider messageSubjectProvider,
            Func<Exception, Message, bool> handleException = null)
        {
            Client = client;
            _serializationRegister = serializationRegister;
            _logger = loggerFactory.CreateLogger("JustSaying.Publish");
            _handleException = handleException;
            _messageSubjectProvider = messageSubjectProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task PublishAsync(Message message, CancellationToken cancellationToken)
            => PublishAsync(message, null, cancellationToken);

        public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
        {
            var request = BuildPublishRequest(message, metadata);
            PublishResponse response = null;
            try
            {
                response = await Client.PublishAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonServiceException ex)
            {
                if (!ClientExceptionHandler(ex, message))
                {
                    throw new PublishException(
                        $"Failed to publish message to SNS. Topic ARN: '{request.TopicArn}', Subject: '{request.Subject}', Message: '{request.Message}'.",
                        ex);
                }
            }

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["AwsRequestId"] = response?.MessageId
            }))
            {
                _logger.LogInformation(
                    "Published message {MessageId} of type {MessageType} to {DestinationType} '{MessageDestination}'.",
                    message.Id,
                    message.GetType().FullName,
                    "Topic",
                    request.TopicArn);
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

        private bool ClientExceptionHandler(Exception ex, Message message) => _handleException?.Invoke(ex, message) ?? false;

        private PublishRequest BuildPublishRequest(Message message, PublishMetadata metadata)
        {
            var messageToSend = _serializationRegister.Serialize(message, serializeForSnsPublishing: true);
            var messageType = _messageSubjectProvider.GetSubjectForType(message.GetType());

            return new PublishRequest
            {
                TopicArn = Arn,
                Subject = messageType,
                Message = messageToSend,
                MessageAttributes = BuildMessageAttributes(metadata)
            };
        }

        private static Dictionary<string, MessageAttributeValue> BuildMessageAttributes(PublishMetadata metadata)
        {
            if (metadata?.MessageAttributes == null || metadata.MessageAttributes.Count == 0)
            {
                return null;
            }
            return metadata.MessageAttributes.ToDictionary(
                source => source.Key,
                source => BuildMessageAttributeValue(source.Value));
        }

        private static MessageAttributeValue BuildMessageAttributeValue(Messaging.MessageAttributeValue value)
        {
            if (value == null)
            {
                return null;
            }

            var binaryValueStream = value.BinaryValue != null
                ? new MemoryStream(value.BinaryValue.ToArray(), false)
                : null;

            return new MessageAttributeValue
            {
                StringValue = value.StringValue,
                BinaryValue = binaryValueStream,
                DataType = value.DataType
            };
        }

        public virtual InterrogationResult Interrogate()
        {
            return new InterrogationResult(InterrogationResult.Empty);
        }
    }
}
