using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using Message = JustSaying.Models.Message;
using SQSMessage = Amazon.SQS.Model.Message;
using Microsoft.Extensions.Logging;

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
        private readonly IMessageContextAccessor _messageContextWriter;

        private static ILogger _log;

        public MessageDispatcher(
            SqsQueueBase queue,
            IMessageSerializationRegister serializationRegister,
            IMessageMonitor messagingMonitor,
            Action<Exception, SQSMessage> onError,
            HandlerMap handlerMap,
            ILoggerFactory loggerFactory,
            IMessageBackoffStrategy messageBackoffStrategy,
            IMessageContextAccessor messageContextWriter)
        {
            _queue = queue;
            _serializationRegister = serializationRegister;
            _messagingMonitor = messagingMonitor;
            _onError = onError;
            _handlerMap = handlerMap;
            _log = loggerFactory.CreateLogger("JustSaying");
            _messageBackoffStrategy = messageBackoffStrategy;
            _messageContextWriter = messageContextWriter;
        }

        public async Task DispatchMessage(SQSMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Message typedMessage;
            try
            {
                typedMessage = _serializationRegister.DeserializeMessage(message.Body);
            }
            catch (MessageFormatNotSupportedException ex)
            {
                _log.LogTrace($"Didn't handle message [{message.Body ?? string.Empty}]. No serializer setup");
                await DeleteMessageFromQueue(message.ReceiptHandle).ConfigureAwait(false);
                _onError(ex, message);
                return;
            }
            catch (Exception ex)
            {
                _log.LogError(0, ex, "Error deserializing message");
                _onError(ex, message);
                return;
            }

            var handlingSucceeded = false;
            Exception lastException = null;

            try
            {
                if (typedMessage != null)
                {
                    _messageContextWriter.MessageContext = new MessageContext(message, _queue.Uri);

                    typedMessage.ReceiptHandle = message.ReceiptHandle;
                    typedMessage.QueueUri = _queue.Uri;

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

                _messageContextWriter.MessageContext = null;
            }
        }

        private async Task<bool> CallMessageHandler(Message message)
        {
            var handler = _handlerMap.Get(message.GetType());

            if (handler == null)
            {
                return true;
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();

            var handlerSucceeded = await handler(message).ConfigureAwait(false);

            watch.Stop();
            _log.LogTrace($"Handled message - MessageType: {message.GetType()}");
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
        
        private async Task UpdateMessageVisibilityTimeout(SQSMessage message, string receiptHandle, Message typedMessage, Exception lastException)
        {
            if (TryGetApproxReceiveCount(message.Attributes, out int approxReceiveCount))
            {
                var visibilityTimeoutSeconds = (int)_messageBackoffStrategy.GetBackoffDuration(typedMessage, approxReceiveCount, lastException).TotalSeconds;

                try
                {
                    var visibilityRequest = new ChangeMessageVisibilityRequest
                    {
                        QueueUrl = _queue.Uri.AbsoluteUri,
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
    }
}
