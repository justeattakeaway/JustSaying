using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using Message = JustSaying.Models.Message;
using SQSMessage = Amazon.SQS.Model.Message;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MessageAttributeValue = JustSaying.Models.MessageAttributeValue;

namespace JustSaying.AwsTools.MessageHandling
{
    public class MessageDispatcher
    {
        private readonly SqsQueueBase _queue;
        private readonly IMessageSerialisationRegister _serialisationRegister;
        private readonly IMessageBodyCompression _messageBodyCompression;
        private readonly IMessageMonitor _messagingMonitor;
        private readonly Action<Exception, SQSMessage> _onError;
        private readonly HandlerMap _handlerMap;
        private readonly IMessageBackoffStrategy _messageBackoffStrategy;

        private static ILogger _log;

        public MessageDispatcher(
            SqsQueueBase queue,
            IMessageSerialisationRegister serialisationRegister,
            IMessageBodyCompression messageBodyCompression,
            IMessageMonitor messagingMonitor,
            Action<Exception, SQSMessage> onError,
            HandlerMap handlerMap,
            ILoggerFactory loggerFactory,
            IMessageBackoffStrategy messageBackoffStrategy)
        {
            _queue = queue;
            _serialisationRegister = serialisationRegister;
            _messagingMonitor = messagingMonitor;
            _onError = onError;
            _handlerMap = handlerMap;
            _log = loggerFactory.CreateLogger("JustSaying");
            _messageBackoffStrategy = messageBackoffStrategy;
            _messageBodyCompression = messageBodyCompression;
        }

        public async Task DispatchMessage(SQSMessage message)
        {
            Message typedMessage;
            try
            {
                var body = GetMessageBody(message);
                typedMessage = _serialisationRegister.DeserializeMessage(body);
            }
            catch (MessageFormatNotSupportedException ex)
            {
                _log.LogTrace($"Didn't handle message [{message.Body ?? string.Empty}]. No serialiser setup");
                await DeleteMessageFromQueue(message.ReceiptHandle).ConfigureAwait(false);
                _onError(ex, message);
                return;
            }
            catch (Exception ex)
            {
                _log.LogError(0, ex, "Error deserialising message");
                _onError(ex, message);
                return;
            }

            var handlingSucceeded = false;
            Exception lastException = null;

            try
            {
                if (typedMessage != null)
                {
                    typedMessage.ReceiptHandle = message.ReceiptHandle;
                    typedMessage.QueueUrl = _queue.Url;
                    handlingSucceeded = await CallMessageHandler(typedMessage).ConfigureAwait(false);
                }

                if (handlingSucceeded)
                {
                    await DeleteMessageFromQueue(message.ReceiptHandle).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var errorText = $"Error handling message [{message.Body}]";
                _log.LogError(0, ex, errorText);

                if (typedMessage != null)
                {
                    _messagingMonitor.HandleException(typedMessage.GetType());
                }

                _onError(ex, message);

                lastException = ex;
            }
            finally
            {
                if (!handlingSucceeded && _messageBackoffStrategy != null)
                {
                    await UpdateMessageVisibilityTimeout(message, message.ReceiptHandle, typedMessage, lastException).ConfigureAwait(false);
                }
            }
        }

        private async Task<bool> CallMessageHandler(Message message)
        {
            var handler = _handlerMap.Get(message.GetType());

            if (handler == null)
            {
                _log.LogError("Failed to dispatch. Handler for message of type '{MessageTypeName}' not found in handler map.", message.GetType().FullName);
                return false;
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();

            var handlerSucceeded = await handler(message).ConfigureAwait(false);

            watch.Stop();
            _log.LogTrace($"Handled message - MessageType: {message.GetType()}");
            _messagingMonitor.HandleTime(watch.ElapsedMilliseconds);

            return handlerSucceeded;
        }

        private async Task DeleteMessageFromQueue(string receiptHandle)
        {
            var deleteRequest = new DeleteMessageRequest
            {
                QueueUrl = _queue.Url,
                ReceiptHandle = receiptHandle
            };

            await _queue.Client.DeleteMessageAsync(deleteRequest).ConfigureAwait(false);
        }

        private async Task UpdateMessageVisibilityTimeout(SQSMessage message, string receiptHandle, Message typedMessage, Exception lastException)
        {
            if (TryGetApproxReceiveCount(message.Attributes, out int approxReceiveCount))
            {
                var visibilityTimeoutSeconds = (int)_messageBackoffStrategy.GetBackoffDuration(typedMessage, approxReceiveCount, lastException).TotalSeconds;

                try
                {
                    var visibilityRequest = new ChangeMessageVisibilityRequest
                    {
                        QueueUrl = _queue.Url,
                        ReceiptHandle = receiptHandle,
                        VisibilityTimeout = visibilityTimeoutSeconds
                    };

                    await _queue.Client.ChangeMessageVisibilityAsync(visibilityRequest).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.LogError(0, ex, $"Failed to update message visibility timeout by {visibilityTimeoutSeconds} seconds");
                    _onError(ex, message);
                }
            }
        }

        private static bool TryGetApproxReceiveCount(IDictionary<string, string> attributes, out int approxReceiveCount)
        {
            approxReceiveCount = 0;

            return attributes.TryGetValue(MessageSystemAttributeName.ApproximateReceiveCount, out string rawApproxReceiveCount) && int.TryParse(rawApproxReceiveCount, out approxReceiveCount);
        }

        private string GetMessageBody(SQSMessage message)
        {
            string body = message.Body;
            var attributes = GetMessageAttributes(message);
            var contentEncoding = attributes.Get(MessageAttributeKeys.ContentEncoding);

            if (body is not null && contentEncoding is null)
            {
                return body;
            }

            return _messageBodyCompression.Decompress(body);
        }

        private static MessageAttributes GetMessageAttributes(SQSMessage message)
        {
            return isSnsPayload(message.Body) ? GetMessageAttributes(message.Body) : GetRawMessageAttributes(message);
        }

        private static MessageAttributes GetMessageAttributes(string message)
        {
            var jsonObject = JObject.Parse(message);

            var attributesToken = jsonObject["MessageAttributes"];
            if (attributesToken == null || attributesToken.Type != JTokenType.Object)
            {
                return new MessageAttributes();
            }

            Dictionary<string, MessageAttributeValue> attributes = new Dictionary<string, MessageAttributeValue>();
            foreach (var property in ((JObject)attributesToken).Properties())
            {
                var dataType = property.Value.Value<string>("Type");
                var dataValue = property.Value.Value<string>("Value");

                attributes.Add(property.Name, ParseMessageAttribute(dataType, dataValue));
            }

            return new MessageAttributes(attributes);
        }

        private static MessageAttributes GetRawMessageAttributes(SQSMessage message)
        {
            if (message.MessageAttributes is null)
            {
                return new MessageAttributes();
            }

            Dictionary<string, MessageAttributeValue> rawAttributes = new Dictionary<string, MessageAttributeValue>();
            foreach (var messageMessageAttribute in message.MessageAttributes)
            {
                var dataType = messageMessageAttribute.Value.DataType;
                var dataValue = messageMessageAttribute.Value.StringValue;
                rawAttributes.Add(messageMessageAttribute.Key, ParseMessageAttribute(dataType, dataValue));
            }

            return new MessageAttributes(rawAttributes);
        }

        private static bool isSnsPayload(string body)
        {
            if (body is null)
            {
                return false;
            }

            try
            {
                var jObject = JObject.Parse(body);
                var typeValue = jObject.Value<string>("Type");
                return typeValue == "Notification";
            }
            catch (JsonReaderException)
            {
            }

            return false;
        }

        private static MessageAttributeValue ParseMessageAttribute(string dataType, string dataValue)
        {
            bool isBinary = dataType?.StartsWith("Binary", StringComparison.Ordinal) is true;

            return new MessageAttributeValue()
            {
                DataType = dataType,
                StringValue = !isBinary ? dataValue : null,
                BinaryValue = isBinary ? Convert.FromBase64String(dataValue) : null
            };
        }

        private string ApplyBodyDecompression(string body, MessageAttributeValue contentEncoding)
        {
            return _messageBodyCompression.Decompress(body);
        }
    }
}
