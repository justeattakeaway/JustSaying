using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;
using MessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace JustSaying.AwsTools.MessageHandling
{
    public abstract class SnsTopicBase : IMessagePublisher
    {
        private readonly IMessageSerializationRegister _serializationRegister; // ToDo: Grrr...why is this here even. GET OUT!
        private readonly IMessageSubjectProvider _messageSubjectProvider;
        private readonly SnsWriteConfiguration _snsWriteConfiguration;
        public Action<MessageResponse, object> MessageResponseLogger { get; set; }
        public string Arn { get; protected set; }
        internal ServerSideEncryption ServerSideEncryption { get; set; }
        protected IAmazonSimpleNotificationService Client { get; set; }
        private readonly ILogger _logger;

        protected SnsTopicBase(
            IMessageSerializationRegister serializationRegister,
            ILoggerFactory loggerFactory,
            IMessageSubjectProvider messageSubjectProvider)
        {
            _serializationRegister = serializationRegister;
            _messageSubjectProvider = messageSubjectProvider;
            _logger = loggerFactory.CreateLogger("JustSaying");
        }

        protected SnsTopicBase(
            IMessageSerializationRegister serializationRegister,
            ILoggerFactory loggerFactory,
            SnsWriteConfiguration snsWriteConfiguration,
            IMessageSubjectProvider messageSubjectProvider)
        {
            _serializationRegister = serializationRegister;
            _logger = loggerFactory.CreateLogger("JustSaying");
            _snsWriteConfiguration = snsWriteConfiguration;
            _messageSubjectProvider = messageSubjectProvider;
        }

        public abstract Task<bool> ExistsAsync();

        public async Task PublishAsync<T>(T message, PublishMetadata metadata, CancellationToken cancellationToken)
            where T : class
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

            _logger.LogInformation(
                "Published message with subject '{MessageSubject}' and content '{MessageBody}'.",
                request.Subject,
                request.Message);

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

        private bool ClientExceptionHandler<T>(Exception ex, T message) where T : class => _snsWriteConfiguration?.HandleException?.Invoke(ex, message) ?? false;

        private PublishRequest BuildPublishRequest<T>(T message, PublishMetadata metadata) where T : class
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
    }
}
