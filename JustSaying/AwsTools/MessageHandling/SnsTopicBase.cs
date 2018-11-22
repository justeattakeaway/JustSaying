using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;
using MessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace JustSaying.AwsTools.MessageHandling
{
    public abstract class SnsTopicBase : IMessagePublisher
    {
        private readonly IMessageSerialisationRegister _serialisationRegister; // ToDo: Grrr...why is this here even. GET OUT!
        private readonly IMessageSubjectProvider _messageSubjectProvider;
        private readonly SnsWriteConfiguration _snsWriteConfiguration;
        public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
        public string Arn { get; protected set; }
        protected IAmazonSimpleNotificationService Client { get; set; }
        private readonly ILogger _eventLog;

        protected SnsTopicBase(IMessageSerialisationRegister serialisationRegister, ILoggerFactory loggerFactory, IMessageSubjectProvider messageSubjectProvider)
        {
            _serialisationRegister = serialisationRegister;
            _messageSubjectProvider = messageSubjectProvider;
            _eventLog = loggerFactory.CreateLogger("EventLog");
        }

        protected SnsTopicBase(IMessageSerialisationRegister serialisationRegister,
            ILoggerFactory loggerFactory, SnsWriteConfiguration snsWriteConfiguration,
            IMessageSubjectProvider messageSubjectProvider)
        {
            _serialisationRegister = serialisationRegister;
            _eventLog = loggerFactory.CreateLogger("EventLog");
            _snsWriteConfiguration = snsWriteConfiguration;
            _messageSubjectProvider = messageSubjectProvider;
        }

        public abstract Task<bool> ExistsAsync();
        
        public async Task PublishAsync(PublishEnvelope envelope, CancellationToken cancellationToken)
        {
            var request = BuildPublishRequest(envelope);

            try
            {
                var response = await Client.PublishAsync(request, cancellationToken).ConfigureAwait(false);
                _eventLog.LogInformation($"Published message: '{request.Subject}' with content {request.Message}");

                MessageResponseLogger?.Invoke(new MessageResponse
                {
                    HttpStatusCode = response?.HttpStatusCode,
                    MessageId = response?.MessageId
                }, envelope.Message);
            }
            catch (Exception ex)
            {
                if (!ClientExceptionHandler(ex, envelope.Message))
                    throw new PublishException(
                        $"Failed to publish message to SNS. TopicArn: {request.TopicArn} Subject: {request.Subject} Message: {request.Message}",
                        ex);
            }
        }

        private bool ClientExceptionHandler(Exception ex, Message message) => _snsWriteConfiguration?.HandleException?.Invoke(ex, message) ?? false;

        private PublishRequest BuildPublishRequest(PublishEnvelope envelope)
        {
            var messageToSend = _serialisationRegister.Serialise(envelope.Message, serializeForSnsPublishing: true);
            var messageType = _messageSubjectProvider.GetSubjectForType(envelope.Message.GetType());

            return new PublishRequest
            {
                TopicArn = Arn,
                Subject = messageType,
                Message = messageToSend,
                MessageAttributes = BuildMessageAttributes(envelope)
            };
        }

        private static Dictionary<string, MessageAttributeValue> BuildMessageAttributes(PublishEnvelope envelope)
        {
            if (envelope.MessageAttributes == null || envelope.MessageAttributes.Count == 0)
            {
                return null;
            }

            return envelope.MessageAttributes.ToDictionary(
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
