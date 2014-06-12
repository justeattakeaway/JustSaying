using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageProcessingStrategies;
using JustSaying.Messaging.Monitoring;
using NLog;
using Newtonsoft.Json.Linq;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using Message = JustSaying.Messaging.Messages.Message;

namespace JustSaying.AwsTools
{
    public class SqsNotificationListener : INotificationSubscriber
    {
        private readonly SqsQueueBase _queue;
        private readonly IMessageSerialisationRegister _serialisationRegister;
        private readonly IMessageFootprintStore _messageFootprintStore;
        private readonly IMessageMonitor _messagingMonitor;
        private readonly Action<Exception> _onError;
        private readonly Dictionary<Type, List<Func<Message, bool>>> _handlers;
        private bool _listen = true;
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        private const int MaxAmazonMessageCap = 10;
        private IMessageProcessingStrategy _messageProcessingStrategy;

        public SqsNotificationListener(SqsQueueBase queue, IMessageSerialisationRegister serialisationRegister, IMessageFootprintStore messageFootprintStore, IMessageMonitor messagingMonitor, Action<Exception> onError = null)
        {
            _queue = queue;
            _serialisationRegister = serialisationRegister;
            _messageFootprintStore = messageFootprintStore;
            _messagingMonitor = messagingMonitor;
            _onError = onError ?? (ex => { });
            _handlers = new Dictionary<Type, List<Func<Message, bool>>>();
            _messageProcessingStrategy = new MaximumThroughput();
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

        public void AddMessageHandler<T>(IHandler<T> handler) where T : Message
        {
            List<Func<Message, bool>> handlers;
            if (!_handlers.TryGetValue(typeof(T), out handlers))
            {
                handlers = new List<Func<Message, bool>>();
                _handlers.Add(typeof(T), handlers);
            }

            handlers.Add(DelegateAdjuster.CastArgument<Message, T>(x => RepeatCallSafe(x, handler)));
        }

        private bool RepeatCallSafe<T>(T message, IHandler<T> handler) where T : Message
        {
            if (_messageFootprintStore.IsMessageReceieved(message.Id))
            {
                return true;
            }

            var result = handler.Handle(message);
            _messageFootprintStore.MarkMessageAsRecieved(message.Id);
            return result;
        }

        public void Listen()
        {
            _listen = true;
            Action run = () => { while (_listen) { ListenLoop(); } };
            run.BeginInvoke(null, null);

            Log.Info("Starting Listening - Queue: " + _queue.QueueNamePrefix);
        }

        public void StopListening()
        {
            _listen = false;
            Log.Info("Stopped Listening - Queue: " + _queue.QueueNamePrefix);
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
                    MaxNumberOfMessages = MaxAmazonMessageCap,
                    WaitTimeSeconds = 20
                });

                watch.Stop();

                _messagingMonitor.ReceiveMessageTime(watch.ElapsedMilliseconds);

                var messageCount = sqsMessageResponse.Messages.Count;

                Log.Trace(string.Format("Polled for messages - Queue: {0}, MessageCount: {1}", _queue.QueueNamePrefix, messageCount));

                foreach (var message in sqsMessageResponse.Messages)
                {
                    HandleMessage(message);
                }
            }
            catch (InvalidOperationException ex)
            {
                Log.Trace("Suspected no messaged in queue. Ex: {0}", ex);
            }
            catch (Exception ex)
            {
                Log.ErrorException("Issue in message handling loop", ex);
            }
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
                typedMessage = _serialisationRegister
                    .GetSerialiser(messageType)
                    .Deserialise(rawMessage);

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
                _onError(ex);
            }
            catch (Exception ex)
            {
                Log.ErrorException(string.Format("Issue handling message... {0}. StackTrace: {1}", message, ex.StackTrace), ex);
                if (typedMessage != null)
                {
                    _messagingMonitor.HandleException(typedMessage.GetType().Name);
                }
                _onError(ex);
                
            }
        }
    }
}