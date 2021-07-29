using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling.Dispatch
{
    public class MessageDispatcher : IMessageDispatcher
    {
        private readonly IMessageSerializationRegister _serializationRegister;
        private readonly IMessageMonitor _messagingMonitor;
        private readonly MiddlewareMap _middlewareMap;
        private readonly IMessageBackoffStrategy _messageBackoffStrategy;
        private readonly IMessageContextAccessor _messageContextAccessor;

        private static ILogger _logger;

        public MessageDispatcher(
            IMessageSerializationRegister serializationRegister,
            IMessageMonitor messagingMonitor,
            MiddlewareMap middlewareMap,
            ILoggerFactory loggerFactory,
            IMessageBackoffStrategy messageBackoffStrategy,
            IMessageContextAccessor messageContextAccessor)
        {
            _serializationRegister = serializationRegister;
            _messagingMonitor = messagingMonitor;
            _middlewareMap = middlewareMap;
            _logger = loggerFactory.CreateLogger("JustSaying");
            _messageBackoffStrategy = messageBackoffStrategy;
            _messageContextAccessor = messageContextAccessor;
        }

        public async Task DispatchMessageAsync(
            IQueueMessageContext messageContext,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            (bool success, Message typedMessage, MessageAttributes attributes) =
                await DeserializeMessage(messageContext, cancellationToken).ConfigureAwait(false);

            if (!success)
            {
                return;
            }

            var handlingSucceeded = false;
            Exception lastException = null;

            try
            {
                _messageContextAccessor.MessageContext =
                    new MessageContext(messageContext.Message, messageContext.QueueUri, attributes);

                handlingSucceeded = await RunMiddleware(messageContext, typedMessage, cancellationToken)
                    .ConfigureAwait(false);
            }

#pragma warning disable CA1031
            catch (Exception ex) when (!(ex is OperationCanceledException))
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

                _messagingMonitor.HandleError(ex, messageContext.Message);

                lastException = ex;
            }
            finally
            {
                try
                {
                    _messagingMonitor.Handled(typedMessage);

                    if (!handlingSucceeded && _messageBackoffStrategy != null)
                    {
                        await UpdateMessageVisibilityTimeout(messageContext,
                            typedMessage,
                            lastException,
                            cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _messageContextAccessor.MessageContext = null;
                }
            }
        }

        private async Task<(bool success, Message typedMessage, MessageAttributes attributes)>
            DeserializeMessage(IQueueMessageContext messageContext, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Attempting to deserialize message with serialization register {Type}",
                    _serializationRegister.GetType().FullName);
                var messageWithAttributes = _serializationRegister.DeserializeMessage(messageContext.Message.Body);
                return (true, messageWithAttributes.Message, messageWithAttributes.MessageAttributes);
            }
            catch (MessageFormatNotSupportedException ex)
            {
                _logger.LogTrace(ex,
                    "Could not handle message with Id '{MessageId}' because a deserializer for the content is not configured. Message body: '{MessageBody}'.",
                    messageContext.Message.MessageId,
                    messageContext.Message.Body);

                await messageContext.DeleteMessage(cancellationToken).ConfigureAwait(false);
                _messagingMonitor.HandleError(ex, messageContext.Message);

                return (false, null, null);
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

                _messagingMonitor.HandleError(ex, messageContext.Message);

                return (false, null, null);
            }
        }

        private async Task<bool> RunMiddleware(IQueueMessageContext context, Message justSayingMessage, CancellationToken cancellationToken)
        {
            var messageType = justSayingMessage.GetType();

            var middleware = _middlewareMap.Get(context.QueueName, messageType);

            if (middleware == null)
            {
                _logger.LogError(
                    "Failed to dispatch. Middleware for message of type '{MessageTypeName}' not found in middleware map.",
                    justSayingMessage.GetType().FullName);
                return false;
            }

            using (_messagingMonitor.MeasureDispatch())
            {
                var handleContext = new HandleMessageContext(context.QueueName, context.Message, justSayingMessage, messageType, context, context);

                return await middleware.RunAsync(handleContext, null, cancellationToken)
                        .ConfigureAwait(false);
            }
        }

        private async Task UpdateMessageVisibilityTimeout(
            IQueueMessageContext messageContext,
            Message typedMessage,
            Exception lastException,
            CancellationToken cancellationToken)
        {
            if (TryGetApproxReceiveCount(messageContext.Message.Attributes, out int approxReceiveCount))
            {
                var visibilityTimeout =
                    _messageBackoffStrategy.GetBackoffDuration(typedMessage,
                        approxReceiveCount,
                        lastException);

                try
                {
                    await messageContext.UpdateMessageVisibility(visibilityTimeout, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (AmazonServiceException ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to update message visibility timeout by {VisibilityTimeout} seconds for message with receipt handle '{ReceiptHandle}'.",
                        visibilityTimeout,
                        messageContext.Message.ReceiptHandle);

                    _messagingMonitor.HandleError(ex, messageContext.Message);
                }
            }
        }

        private static bool TryGetApproxReceiveCount(
            IDictionary<string, string> attributes,
            out int approxReceiveCount)
        {
            approxReceiveCount = 0;

            return attributes.TryGetValue(MessageSystemAttributeName.ApproximateReceiveCount,
                    out string rawApproxReceiveCount) &&
                int.TryParse(rawApproxReceiveCount,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out approxReceiveCount);
        }
    }
}
