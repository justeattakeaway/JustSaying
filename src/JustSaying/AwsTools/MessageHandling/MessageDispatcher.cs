using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.AwsTools.MessageHandling
{
    public class MessageDispatcher
    {
        private readonly SqsQueueBase _queue;
        private readonly IMessageSerializationRegister _serializationRegister;
        private readonly IMessageMonitor _messagingMonitor;
        private readonly Action<Exception, SQSMessage> _onError;
        private readonly HandlerMap _handlerMap;
        private readonly IMessageBackoffStrategy _messageBackoffStrategy;
        private readonly IMessageContextAccessor _messageContextAccessor;

        private static ILogger _logger;

        public MessageDispatcher(
            SqsQueueBase queue,
            IMessageSerializationRegister serializationRegister,
            IMessageMonitor messagingMonitor,
            Action<Exception, SQSMessage> onError,
            HandlerMap handlerMap,
            ILoggerFactory loggerFactory,
            IMessageBackoffStrategy messageBackoffStrategy,
            IMessageContextAccessor messageContextAccessor)
        {
            _queue = queue;
            _serializationRegister = serializationRegister;
            _messagingMonitor = messagingMonitor;
            _onError = onError;
            _handlerMap = handlerMap;
            _logger = loggerFactory.CreateLogger("JustSaying");
            _messageBackoffStrategy = messageBackoffStrategy;
            _messageContextAccessor = messageContextAccessor;
        }

        public async Task DispatchMessage(SQSMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            object untypedMessage;
            try
            {
                untypedMessage = _serializationRegister.DeserializeMessage(message.Body);
            }
            catch (MessageFormatNotSupportedException ex)
            {
                _logger.LogTrace(
                    "Could not handle message with Id '{MessageId}' because a deserializer for the content is not configured. Message body: '{MessageBody}'.",
                    message.MessageId,
                    message.Body);

                await DeleteMessageFromQueue(message.ReceiptHandle).ConfigureAwait(false);
                _onError(ex, message);

                return;
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(
                    ex,
                    "Error deserializing message with Id '{MessageId}' and body '{MessageBody}'.",
                    message.MessageId,
                    message.Body);

                _onError(ex, message);
                return;
            }

            var handlingSucceeded = false;
            Exception lastException = null;

            try
            {
                if (untypedMessage != null)
                {
                    _messageContextAccessor.MessageContext = new MessageContext(message, _queue.Uri);

                    handlingSucceeded = await CallMessageHandler(untypedMessage).ConfigureAwait(false);
                }

                if (handlingSucceeded)
                {
                    await DeleteMessageFromQueue(message.ReceiptHandle).ConfigureAwait(false);
                }
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(
                    ex,
                    "Error handling message with Id '{MessageId}' and body '{MessageBody}'.",
                    message.MessageId,
                    message.Body);

                if (untypedMessage != null)
                {
                    _messagingMonitor.HandleException(untypedMessage.GetType());
                }

                _onError(ex, message);

                lastException = ex;
            }
            finally
            {
                try
                {
                    if (!handlingSucceeded && _messageBackoffStrategy != null)
                    {
                        await UpdateMessageVisibilityTimeout(message, message.ReceiptHandle, untypedMessage, lastException).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _messageContextAccessor.MessageContext = null;
                }
            }
        }

        private async Task<bool> CallMessageHandler(object message)
        {
            var messageType = message.GetType();

            var handler = _handlerMap.Get(messageType);

            if (handler == null)
            {
                return true;
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();

            var handlerSucceeded = await handler(message).ConfigureAwait(false);

            watch.Stop();

            _logger.LogTrace(
                "Handled message of type {MessageType} in {TimeToHandle}.",
                messageType,
                watch.Elapsed);

            _messagingMonitor.HandleTime(watch.Elapsed);

            return handlerSucceeded;
        }

        private async Task DeleteMessageFromQueue(string receiptHandle)
        {
            var deleteRequest = new DeleteMessageRequest
            {
                QueueUrl = _queue.Uri.AbsoluteUri,
                ReceiptHandle = receiptHandle
            };

            await _queue.Client.DeleteMessageAsync(deleteRequest).ConfigureAwait(false);
        }

        private async Task UpdateMessageVisibilityTimeout(SQSMessage message, string receiptHandle, object untypedMessage, Exception lastException)
        {
            if (TryGetApproxReceiveCount(message.Attributes, out int approxReceiveCount))
            {
                var visibilityTimeout = _messageBackoffStrategy.GetBackoffDuration(untypedMessage, approxReceiveCount, lastException);
                var visibilityTimeoutSeconds = (int)visibilityTimeout.TotalSeconds;

                var visibilityRequest = new ChangeMessageVisibilityRequest
                {
                    QueueUrl = _queue.Uri.AbsoluteUri,
                    ReceiptHandle = receiptHandle,
                    VisibilityTimeout = visibilityTimeoutSeconds
                };

                try
                {
                    await _queue.Client.ChangeMessageVisibilityAsync(visibilityRequest).ConfigureAwait(false);
                }
                catch (AmazonServiceException ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to update message visibility timeout by {VisibilityTimeout} seconds for message with receipt handle '{ReceiptHandle}'.",
                        visibilityTimeoutSeconds,
                        receiptHandle);

                    _onError(ex, message);
                }
            }
        }

        private static bool TryGetApproxReceiveCount(IDictionary<string, string> attributes, out int approxReceiveCount)
        {
            approxReceiveCount = 0;

            return attributes.TryGetValue(MessageSystemAttributeName.ApproximateReceiveCount, out string rawApproxReceiveCount) &&
                   int.TryParse(rawApproxReceiveCount, out approxReceiveCount);
        }
    }
}
