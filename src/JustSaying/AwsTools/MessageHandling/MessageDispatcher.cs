using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging.Channels;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.AwsTools.MessageHandling
{
    public interface IMessageDispatcher
    {
        Task DispatchMessage(IQueueMessageContext messageContext, CancellationToken cancellationToken);
    }

    public class MessageDispatcher : IMessageDispatcher
    {
        private readonly IMessageSerializationRegister _serializationRegister;
        private readonly IMessageMonitor _messagingMonitor;
        private readonly Action<Exception, SQSMessage> _onError;
        private readonly HandlerMap _handlerMap;
        private readonly IMessageBackoffStrategy _messageBackoffStrategy;
        private readonly IMessageContextAccessor _messageContextAccessor;

        private static ILogger _logger;

        public MessageDispatcher(
            IMessageSerializationRegister serializationRegister,
            IMessageMonitor messagingMonitor,
            Action<Exception, SQSMessage> onError,
            HandlerMap handlerMap,
            ILoggerFactory loggerFactory,
            IMessageBackoffStrategy messageBackoffStrategy,
            IMessageContextAccessor messageContextAccessor)
        {
            _serializationRegister = serializationRegister;
            _messagingMonitor = messagingMonitor;
            _onError = onError ?? DefaultErrorHandler;
            _handlerMap = handlerMap;
            _logger = loggerFactory.CreateLogger("JustSaying");
            _messageBackoffStrategy = messageBackoffStrategy;
            _messageContextAccessor = messageContextAccessor;
        }

        public async Task DispatchMessage(IQueueMessageContext messageContext, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Message typedMessage;
            try
            {
                typedMessage = _serializationRegister.DeserializeMessage(messageContext.Message.Body);
            }
            catch (MessageFormatNotSupportedException ex)
            {
                _logger.LogTrace(
                    "Could not handle message with Id '{MessageId}' because a deserializer for the content is not configured. Message body: '{MessageBody}'.",
                    messageContext.Message.MessageId,
                    messageContext.Message.Body);

                await messageContext.DeleteMessageFromQueue().ConfigureAwait(false);
                _onError(ex, messageContext.Message);

                return;
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(
                    ex,
                    "Error deserializing message with Id '{MessageId}' and body '{MessageBody}'.",
                    messageContext.Message.MessageId,
                    messageContext.Message.Body);

                _onError(ex, messageContext.Message);
                return;
            }

            var handlingSucceeded = false;
            Exception lastException = null;

            try
            {
                if (typedMessage != null)
                {
                    _messageContextAccessor.MessageContext = new MessageContext(messageContext.Message, messageContext.QueueUri);

                    handlingSucceeded = await CallMessageHandler(typedMessage).ConfigureAwait(false);
                }

                if (handlingSucceeded)
                {
                    await messageContext.DeleteMessageFromQueue().ConfigureAwait(false);
                }
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(
                    ex,
                    "Error handling message with Id '{MessageId}' and body '{MessageBody}'.",
                    messageContext.Message.MessageId,
                    messageContext.Message.Body);

                if (typedMessage != null)
                {
                    _messagingMonitor.HandleException(typedMessage.GetType());
                }

                _onError(ex, messageContext.Message);

                lastException = ex;
            }
            finally
            {
                try
                {
                    if (!handlingSucceeded && _messageBackoffStrategy != null)
                    {
                        await UpdateMessageVisibilityTimeout(messageContext, typedMessage, lastException).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _messageContextAccessor.MessageContext = null;
                }
            }
        }

        private async Task<bool> CallMessageHandler(Message message)
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

            _logger.LogDebug(
                "Handled message with Id '{MessageId}' of type {MessageType} in {TimeToHandle}.",
                message.Id,
                messageType,
                watch.Elapsed);

            _messagingMonitor.HandleTime(watch.Elapsed);

            return handlerSucceeded;
        }

        private async Task UpdateMessageVisibilityTimeout(IQueueMessageContext messageContext, Message typedMessage, Exception lastException)
        {
            if (TryGetApproxReceiveCount(messageContext.Message.Attributes, out int approxReceiveCount))
            {
                var visibilityTimeout = _messageBackoffStrategy.GetBackoffDuration(typedMessage, approxReceiveCount, lastException);
                var visibilityTimeoutSeconds = (int)visibilityTimeout.TotalSeconds;

                try
                {
                    await messageContext.ChangeMessageVisibilityAsync(visibilityTimeoutSeconds).ConfigureAwait(false);
                }
                catch (AmazonServiceException ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to update message visibility timeout by {VisibilityTimeout} seconds for message with receipt handle '{ReceiptHandle}'.",
                        visibilityTimeoutSeconds,
                        messageContext.Message.ReceiptHandle);

                    _onError(ex, messageContext.Message);
                }
            }
        }

        private static bool TryGetApproxReceiveCount(IDictionary<string, string> attributes, out int approxReceiveCount)
        {
            approxReceiveCount = 0;

            return attributes.TryGetValue(MessageSystemAttributeName.ApproximateReceiveCount, out string rawApproxReceiveCount) &&
                   int.TryParse(rawApproxReceiveCount, out approxReceiveCount);
        }

        private static void DefaultErrorHandler(Exception exception, Amazon.SQS.Model.Message message)
        {
            // No-op
        }
    }
}
