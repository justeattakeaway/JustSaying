using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using NLog;
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
        private readonly HandlerMap _handlerMap = new HandlerMap();
        private readonly IMessageLock _messageLock;

        private CancellationTokenSource _cts = new CancellationTokenSource();
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        private const int MaxAmazonMessageCap = 10;
        private IMessageProcessingStrategy _messageProcessingStrategy;

        public SqsNotificationListener(SqsQueueBase queue, IMessageSerialisationRegister serialisationRegister, IMessageMonitor messagingMonitor, Action<Exception, Amazon.SQS.Model.Message> onError = null, IMessageLock messageLock = null)
        {
            _queue = queue;
            _serialisationRegister = serialisationRegister;
            _messagingMonitor = messagingMonitor;
            _onError = onError ?? ((ex,message) => { });
            _messageProcessingStrategy = new MaximumThroughput();
            _messageLock = messageLock;
            Subscribers = new Collection<ISubscriber>();
        }

        public string Queue
        {
            get { return _queue.QueueName; }
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
            var handlerInstance = futureHandler();
            var guaranteedDelivery = new GuaranteedOnceDelivery<T>(handlerInstance);
            
            IHandler<T> handler = new FutureHandler<T>(futureHandler);
            if (guaranteedDelivery.Enabled)
            {
                if (_messageLock == null)
                {
                    throw new Exception("IMessageLock is null. You need to specify an implementation for IMessageLock.");
                }

                handler = new ExactlyOnceHandler<T>(handler, _messageLock, guaranteedDelivery.TimeOut, handlerInstance.GetType().FullName.ToLower());
            }
            var executionTimeMonitoring = _messagingMonitor as IMeasureHandlerExecutionTime;
            if (executionTimeMonitoring != null)
            {
                handler = new StopwatchHandler<T>(handler, executionTimeMonitoring);
            }

            Subscribers.Add(new Subscriber(typeof(T)));
            _handlerMap.Add(typeof(T), message => handler.Handle((T)message));
        }

        public void Listen()
        {
            var queue = _queue.QueueName;
            var region = _queue.Region.SystemName;
            var queueInfo = string.Format("Queue: {0}, Region: {1}", queue, region);

            _cts = new CancellationTokenSource();
            Task.Factory.StartNew(async () =>
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        await ListenLoop(_cts.Token);
                    }
                })
                .ContinueWith(t =>
                    {
                        if (t.IsCompleted)
                        {
                            Log.Info(
                                "[Completed] Stopped Listening - {0}", 
                                queueInfo);
                        }
                        else if (t.IsFaulted)
                        {
                            Log.Info(
                                "[Failed] Stopped Listening - {0}\n{1}", 
                                queueInfo, 
                                t.Exception);
                        }
                        else
                        {
                            Log.Info(
                                "[Canceled] Stopped Listening - {0}", 
                                queueInfo);
                        }
                    });

            Log.Info(
                "Starting Listening - {0}", 
                queueInfo);
        }

        public void StopListening()
        {
            _cts.Cancel();
            Log.Info(
                "Stopped Listening - Queue: {0}, Region: {1}",
                _queue.QueueName,
                _queue.Region.SystemName);
        }

        internal async Task ListenLoop(CancellationToken ct)
        {
            var queueName = _queue.QueueName;
            var region = _queue.Region.SystemName;

            try
            {
                _messageProcessingStrategy.BeforeGettingMoreMessages();

                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                var request = new ReceiveMessageRequest
                    {
                        QueueUrl = _queue.Url,
                        MaxNumberOfMessages = GetMaxBatchSize(),
                        WaitTimeSeconds = 20
                    };
                var sqsMessageResponse = await _queue.Client.ReceiveMessageAsync(request, ct);

                watch.Stop();

                _messagingMonitor.ReceiveMessageTime(watch.ElapsedMilliseconds);

                var messageCount = sqsMessageResponse.Messages.Count;

                Log.Trace(
                    "Polled for messages - Queue: {0}, Region: {1}, MessageCount: {2}",
                    queueName,
                    region,
                    messageCount);

                foreach (var message in sqsMessageResponse.Messages)
                {
                    HandleMessage(message);
                }
            }
            catch (InvalidOperationException ex)
            {
                Log.Trace(
                    "Suspected no message in queue [{0}], region: [{1}]. Ex: {2}",
                    queueName,
                    region,
                    ex);
            }
            catch (Exception ex)
            {
                var msg = string.Format(
                    "Issue in message handling loop for queue {0}, region {1}",
                    queueName,
                    region);
                Log.Error(ex, msg);
            }
        }

        private int GetMaxBatchSize()
        {
            var maxMessageBatchSize = _messageProcessingStrategy as IMessageMaxBatchSizeProcessingStrategy;

            if (maxMessageBatchSize == null || 
                maxMessageBatchSize.MaxBatchSize <= 0 || 
                maxMessageBatchSize.MaxBatchSize > MaxAmazonMessageCap)
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
                    handlingSucceeded = CallMessageHandlers(typedMessage);
                }

                if (handlingSucceeded)
                {
                    DeleteMessageFromQueue(message.ReceiptHandle);
                }
            }
            catch (Exception ex)
            {
                var msg = string.Format(
                    "Issue handling message... {0}. StackTrace: {1}",
                    message,
                    ex.StackTrace);
                Log.Error(ex, msg);

                if (typedMessage != null)
                {
                    _messagingMonitor.HandleException(typedMessage.GetType().Name);
                }

                _onError(ex, message);
            }
        }

        private bool CallMessageHandlers(Message message)
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

                var thisHandlerSucceeded = handlerFunc(message);
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

        public ICollection<ISubscriber> Subscribers { get; set; }
    }
}