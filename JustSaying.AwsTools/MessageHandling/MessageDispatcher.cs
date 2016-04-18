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
                    "Didn't handle message [{0}]. No serialiser setup",
                    message.Body ?? string.Empty);
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
                    handlingSucceeded = await CallMessageHandlers(typedMessage);
                }

                if (handlingSucceeded)
                {
                    DeleteMessageFromQueue(message.ReceiptHandle);
                }
            }
            catch (Exception ex)
            {
                var errorText = string.Format("Error handling message [{0}]", message.Body);
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
            var handlerFuncs = _handlerMap.Get(message.GetType());

            if (handlerFuncs == null)
            {
                return true;
            }

            var allHandlersSucceeded = true;
            foreach (var handlerFunc in handlerFuncs)
            {
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                var thisHandlerSucceeded = await handlerFunc(message);
                allHandlersSucceeded = allHandlersSucceeded && thisHandlerSucceeded;

                watch.Stop();
                Log.Trace("Handled message - MessageType: {0}", message.GetType().Name);
                _messagingMonitor.HandleTime(watch.ElapsedMilliseconds);
            }

            return allHandlersSucceeded;
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
