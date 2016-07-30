using System;
using System.Threading.Tasks;
using NLog;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Messaging.Monitoring;
using Message = JustSaying.Models.Message;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.AwsTools.MessageHandling
{
    public class MessageDispatcher
    {
        private readonly SqsQueueBase _queue;
        private readonly IMessageSerialisationRegister _serialisationRegister;
        private readonly IMessageMonitor _messagingMonitor;
        private readonly Action<Exception, SQSMessage> _onError;
        private readonly HandlerMap _handlerMap;

        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        public MessageDispatcher(
            SqsQueueBase queue, 
            IMessageSerialisationRegister serialisationRegister,
            IMessageMonitor messagingMonitor,
            Action<Exception, SQSMessage> onError,
            HandlerMap handlerMap)
        {
            _queue = queue;
            _serialisationRegister = serialisationRegister;
            _messagingMonitor = messagingMonitor;
            _onError = onError;
            _handlerMap = handlerMap;
        }

        public async Task DispatchMessage(SQSMessage message)
        {
            Message typedMessage;
            try
            {
                typedMessage = _serialisationRegister.DeserializeMessage(message.Body);
            }
            catch (MessageFormatNotSupportedException ex)
            {
                Log.Trace(
                    $"Didn't handle message [{message.Body ?? string.Empty}]. No serialiser setup");
                DeleteMessageFromQueue(message.ReceiptHandle);
                _onError(ex, message);
                return;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deserialising message");
                _onError(ex, message);
                return;
            }

            try
            {
                var handlingSucceeded = true;

                if (typedMessage != null)
                {
                    handlingSucceeded = await CallMessageHandlers(typedMessage).ConfigureAwait(false);
                }

                if (handlingSucceeded)
                {
                    DeleteMessageFromQueue(message.ReceiptHandle);
                }
            }
            catch (Exception ex)
            {
                var errorText = $"Error handling message [{message.Body}]";
                Log.Error(ex, errorText);

                if (typedMessage != null)
                {
                    _messagingMonitor.HandleException(typedMessage.GetType().Name);
                }

                _onError(ex, message);
            }
        }

        private async Task<bool> CallMessageHandlers(Message message)
        {
            var handler = _handlerMap.Get(message.GetType());

            if (handler == null)
            {
                return true;
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();

            var handlerSucceeded = await handler(message).ConfigureAwait(false);

            watch.Stop();
            Log.Trace($"Handled message - MessageType: {message.GetType().Name}");
            _messagingMonitor.HandleTime(watch.ElapsedMilliseconds);

            return handlerSucceeded;
        }

        private void DeleteMessageFromQueue(string receiptHandle)
        {
            var deleteRequest = new DeleteMessageRequest
            {
                QueueUrl = _queue.Url,
                ReceiptHandle = receiptHandle
            };
            _queue.Client.DeleteMessage(deleteRequest);
        }
    }
}
