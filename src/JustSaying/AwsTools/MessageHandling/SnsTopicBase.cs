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
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;
using MessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace JustSaying.AwsTools.MessageHandling
{
    public abstract class SnsTopicBase : IMessagePublisher, IInterrogable
    {
        private readonly IMessageSerializationRegister _serializationRegister; // ToDo: Grrr...why is this here even. GET OUT!
        private readonly IMessageSubjectProvider _messageSubjectProvider;
        private readonly SnsWriteConfiguration _snsWriteConfiguration;
        public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
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

        private bool ClientExceptionHandler(Exception ex, Message message) => _snsWriteConfiguration?.HandleException?.Invoke(ex, message) ?? false;

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
