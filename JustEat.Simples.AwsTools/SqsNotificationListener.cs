using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
using JustEat.Simples.NotificationStack.AwsTools.MessageProcessingStrategies;
using JustEat.Simples.NotificationStack.AwsTools.MessageProcessingStrategies.JustEat.Simples.NotificationStack.AwsTools.MessageProcessingStrategies;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using NLog;
using Newtonsoft.Json.Linq;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using Message = JustEat.Simples.NotificationStack.Messaging.Messages.Message;

namespace JustEat.Simples.NotificationStack.AwsTools
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
        private static readonly Logger Log = LogManager.GetLogger("JustEat.Simples.NotificationStack");

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

        public SqsNotificationListener WithMaximumConcurrentLimitOnMessagesInFlightOf(int maximumAllowedMesagesInFlight)
        {
            _messageProcessingStrategy = new Throttled(maximumAllowedMesagesInFlight, MaxAmazonMessageCap, _messagingMonitor);
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

                var sqsMessageResponse = _queue.Client.ReceiveMessage(new ReceiveMessageRequest()
                    .WithQueueUrl(_queue.Url)
                    .WithMaxNumberOfMessages(MaxAmazonMessageCap)
                    .WithWaitTimeSeconds(20));

                var messageCount = (sqsMessageResponse.IsSetReceiveMessageResult())
                    ? sqsMessageResponse.ReceiveMessageResult.Message.Count
                    : 0;

                Log.Trace(string.Format("Polled for messages - Queue: {0}, MessageCount: {1}", _queue.QueueNamePrefix, messageCount));

                foreach (var message in sqsMessageResponse.ReceiveMessageResult.Message)
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
                _messagingMonitor.HandleException();
            }
        }

        public void HandleMessage(Amazon.SQS.Model.Message message)
        {
            var action = new Action(() => ProcessMessageAction(message));
            _messageProcessingStrategy.ProcessMessage(action);
        }

        public void ProcessMessageAction(Amazon.SQS.Model.Message message)
        {
            try
            {
                var messageType = JObject.Parse(message.Body)["Subject"].ToString();

                var typedMessage = _serialisationRegister
                            .GetSerialiser(messageType)
                            .Deserialise(JObject.Parse(message.Body)["Message"].ToString());

                var handlingSucceeded = true;

                if (typedMessage != null)
                {
                    List<Func<Message, bool>> handlers;
                    if (!_handlers.TryGetValue(typedMessage.GetType(), out handlers)) return;

                    foreach (var handle in handlers)
                    {
                        var watch = new System.Diagnostics.Stopwatch();
                        watch.Start();

                        if (!handle(typedMessage))
                            handlingSucceeded = false;

                        watch.Stop();
                        Log.Trace("Handled message - MessageType: " + messageType);
                        _messagingMonitor.HandleTime(watch.ElapsedMilliseconds);
                    }
                }

                if (handlingSucceeded)
                    _queue.Client.DeleteMessage(new DeleteMessageRequest().WithQueueUrl(_queue.Url).WithReceiptHandle(message.ReceiptHandle));

            }
            catch (KeyNotFoundException)
            {
                Log.Trace("Didn't handle message {0}. No serialiser setup", JObject.Parse(message.Body)["Subject"].ToString());
                _queue.Client.DeleteMessage(new DeleteMessageRequest().WithQueueUrl(_queue.Url).WithReceiptHandle(message.ReceiptHandle));
            }
            catch (Exception ex)
            {
                Log.ErrorException(string.Format("Issue handling message... {0}. StackTrace: {1}", message, ex.StackTrace), ex);
                _onError(ex);
            }
        }
    }
}