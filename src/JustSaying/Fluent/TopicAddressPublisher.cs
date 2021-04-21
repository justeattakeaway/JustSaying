using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using MessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace JustSaying.Fluent
{
    /// <summary>
    ///
    /// </summary>
    internal sealed class TopicAddressPublisher : IMessagePublisher
    {
        private readonly IAmazonSimpleNotificationService _snsClient;
        private readonly IMessageSubjectProvider _subjectProvider;
        private readonly IMessageSerializationRegister _serializationRegister;
        private readonly string _topicArn;
        private readonly ILogger _logger;
        private readonly Func<Exception, Message, bool> _handleException;

        public TopicAddressPublisher(IAmazonSimpleNotificationService snsClient, ILoggerFactory loggerFactory, IMessageSubjectProvider subjectProvider, IMessageSerializationRegister serializationRegister, Func<Exception, Message, bool> handleException, string topicArn)
        {
            _snsClient = snsClient;
            _subjectProvider = subjectProvider;
            _serializationRegister = serializationRegister;
            _topicArn = topicArn;
            _handleException = handleException;
            _logger = loggerFactory.CreateLogger("JustSaying");
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public Task PublishAsync(Message message, CancellationToken cancellationToken)
        {
            return PublishAsync(message, null, cancellationToken);
        }

        public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
        {
            var request = BuildPublishRequest(message, metadata);
            PublishResponse response = null;
            try
            {
                response = await _snsClient.PublishAsync(request, cancellationToken).ConfigureAwait(false);
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
                "Published message: '{SnsSubject}' with content {SnsMessage} and request Id '{SnsRequestId}'",
                request.Subject,
                request.Message,
                response?.ResponseMetadata?.RequestId);
        }

        private bool ClientExceptionHandler(Exception ex, Message message) => _handleException?.Invoke(ex, message) ?? false;

        private PublishRequest BuildPublishRequest(Message message, PublishMetadata metadata)
        {
            var messageToSend = _serializationRegister.Serialize(message, serializeForSnsPublishing: true);
            var messageType = _subjectProvider.GetSubjectForType(message.GetType());
            return new PublishRequest
            {
                TopicArn = _topicArn,
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
        public InterrogationResult Interrogate()
        {
            return InterrogationResult.Empty;
        }
    }
}
