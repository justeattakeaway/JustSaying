using System;
using System.Linq;
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
                    typedMessage.ReceiptHandle = message.ReceiptHandle;
                    typedMessage.QueueUrl = new Uri(_queue.Url);
                    handlingSucceeded = await CallMessageHandlers(typedMessage).ConfigureAwait(false);
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

            if ((handlerFuncs == null) || (handlerFuncs.Count == 0))
            {
                return true;
            }

            bool allHandlersSucceeded;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            if (handlerFuncs.Count == 1)
            {
                // a shortcut for the usual case
                allHandlersSucceeded = await handlerFuncs[0](message).ConfigureAwait(false);
            }
            else
            {
                var handlerTasks = handlerFuncs.Select(func => func(message));
                var handlerResults = await Task.WhenAll(handlerTasks).ConfigureAwait(false);
                allHandlersSucceeded = handlerResults.All(x => x);
            }

            watch.Stop();
            Log.Trace("Handled message - MessageType: {0}", message.GetType().Name);
            _messagingMonitor.HandleTime(watch.ElapsedMilliseconds);

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
