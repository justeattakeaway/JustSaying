using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using NLog;
using Newtonsoft.Json.Linq;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools
{
    public class SqsNotificationListener : INotificationSubscriber
    {
        private readonly SqsQueueBase _queue;
        private readonly IMessageSerialisationRegister _serialisationRegister;
        private readonly IMessageMonitor _messagingMonitor;
        private readonly Action<Exception, Amazon.SQS.Model.Message> _onError;
        private readonly Dictionary<Type, List<Func<Message, bool>>> _handlers;
        private readonly IMessageLock _messageLock;

        private bool _listen = true;
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        private const int MaxAmazonMessageCap = 10;
        private IMessageProcessingStrategy _messageProcessingStrategy;

        public SqsNotificationListener(SqsQueueBase queue, IMessageSerialisationRegister serialisationRegister, IMessageMonitor messagingMonitor, Action<Exception, Amazon.SQS.Model.Message> onError = null, IMessageLock messageLock = null)
        {
            _queue = queue;
            _serialisationRegister = serialisationRegister;
            _messagingMonitor = messagingMonitor;
            _onError = onError ?? ((ex,message) => { });
            _handlers = new Dictionary<Type, List<Func<Message, bool>>>();
            _messageProcessingStrategy = new MaximumThroughput();
            _messageLock = messageLock;
            Subscribers = new Collection<ISubscriber>();
        }

        public string Queue
        {
            get { return this._queue.QueueName; }
        }
        // ToDo: This should not be here.
        public SqsNotificationListener WithMaximumConcurrentLimitOnMessagesInFlightOf(int maximumAllowedMesagesInFlight)
        {
            _messageProcessingStrategy = new Throttled(maximumAllowedMesagesInFlight, MaxAmazonMessageCap, _messagingMonitor);
            return this;
        }

        public SqsNotificationListener WithMessageProcessingStrategy(IMessageProcessingStrategy messageProcessingStrategy)
        {
            _messageProcessingStrategy = messageProcessingStrategy;
            return this;
        }

        public void AddMessageHandler<T>(Func<IHandler<T>> futureHandler) where T : Message
        {
            List<Func<Message, bool>> handlers;
            if (!_handlers.TryGetValue(typeof(T), out handlers))
            {
                handlers = new List<Func<Message, bool>>();
                _handlers.Add(typeof(T), handlers);
            }
            var handlerInstance = futureHandler();
            var guaranteedDelivery = new GuaranteedOnceDelivery<T>(handlerInstance);
            
            IHandler<T> handler = new FutureHandler<T>(futureHandler);
            if (guaranteedDelivery.Enabled)
            {
                if(_messageLock == null)
                    throw new Exception("IMessageLock is null. You need to specify an implementation for IMessageLock.");

                handler = new ExactlyOnceHandler<T>(handler, _messageLock, guaranteedDelivery.TimeOut, handlerInstance.GetType().FullName.ToLower());
            }
            var executionTimeMonitoring = _messagingMonitor as IMeasureHandlerExecutionTime;
            if (executionTimeMonitoring != null)
            {
                handler = new StopwatchHandler<T>(handler, executionTimeMonitoring);
            }

            Subscribers.Add(new Subsriber(typeof(T)));
            handlers.Add(message => handler.Handle((T)message));
        }

        public void Listen()
        {
            _listen = true;
            Action run = () => { while (_listen) { ListenLoop(); } };
            run.BeginInvoke(null, null);

            Log.Info("Starting Listening - Queue: " + _queue.QueueName);
        }

        public void StopListening()
        {
            _listen = false;
            Log.Info("Stopped Listening - Queue: " + _queue.QueueName);
        }

        internal void ListenLoop()
        {
            try
            {
                _messageProcessingStrategy.BeforeGettingMoreMessages();

                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                var sqsMessageResponse = _queue.Client.ReceiveMessage(new ReceiveMessageRequest
                {
                    QueueUrl = _queue.Url,
                    MaxNumberOfMessages = GetMaxBatchSize(),
                    WaitTimeSeconds = 20
                });

                watch.Stop();

                _messagingMonitor.ReceiveMessageTime(watch.ElapsedMilliseconds);

                var messageCount = sqsMessageResponse.Messages.Count;

                Log.Trace(string.Format("Polled for messages - Queue: {0}, MessageCount: {1}", _queue.QueueName, messageCount));

                foreach (var message in sqsMessageResponse.Messages)
                {
                    HandleMessage(message);
                }
            }
            catch (InvalidOperationException ex)
            {
                Log.Trace("Suspected no message in queue {0}. Ex: {1}", _queue.QueueName, ex);
            }
            catch (Exception ex)
            {
                Log.ErrorException(string.Format("Issue in message handling loop for queue {0}", _queue.QueueName), ex);
            }
        }

        private int GetMaxBatchSize()
        {
            var maxMessageBatchSize = _messageProcessingStrategy as IMessageMaxBatchSizeProcessingStrategy;

            if (maxMessageBatchSize == null || (maxMessageBatchSize.MaxBatchSize <= 0 || maxMessageBatchSize.MaxBatchSize > MaxAmazonMessageCap))
            {
                return MaxAmazonMessageCap;
            }
            return maxMessageBatchSize.MaxBatchSize;
        }

        public void HandleMessage(Amazon.SQS.Model.Message message)
        {
            var action = new Action(() => ProcessMessageAction(message));
            _messageProcessingStrategy.ProcessMessage(action);
        }

        public void ProcessMessageAction(Amazon.SQS.Model.Message message)
        {
            Message typedMessage = null;
            string rawMessage = null;
            try
            {
                var body = JObject.Parse(message.Body);
                string messageType = body["Subject"].ToString();

                rawMessage = body["Message"].ToString();
                var typeSerialiser = _serialisationRegister.GeTypeSerialiser(messageType);
                typedMessage = typeSerialiser.Serialiser.Deserialise(rawMessage, typeSerialiser.Type);

                var handlingSucceeded = true;

                if (typedMessage != null)
                {
                    List<Func<Message, bool>> handlers;
                    if (!_handlers.TryGetValue(typedMessage.GetType(), out handlers)) return;

                    foreach (var handle in handlers)
                    {
                        var watch = new System.Diagnostics.Stopwatch();
                        watch.Start();

                        handlingSucceeded = handle(typedMessage);

                        watch.Stop();
                        Log.Trace("Handled message - MessageType: " + messageType);
                        _messagingMonitor.HandleTime(watch.ElapsedMilliseconds);
                    }
                }

                if (handlingSucceeded)
                    _queue.Client.DeleteMessage(new DeleteMessageRequest { QueueUrl = _queue.Url, ReceiptHandle = message.ReceiptHandle });

            }
            catch (KeyNotFoundException ex)
            {
                Log.Trace("Didn't handle message {0}. No serialiser setup", rawMessage ?? "");
                _queue.Client.DeleteMessage(new DeleteMessageRequest
                {
                    QueueUrl = _queue.Url,
                    ReceiptHandle = message.ReceiptHandle
                });
                _onError(ex, message);
            }
            catch (Exception ex)
            {
                Log.ErrorException(string.Format("Issue handling message... {0}. StackTrace: {1}", message, ex.StackTrace), ex);
                if (typedMessage != null)
                {
                    _messagingMonitor.HandleException(typedMessage.GetType().Name);
                }
                _onError(ex, message);
                
            }
        }
        public ICollection<ISubscriber> Subscribers { get; }
    }
}
