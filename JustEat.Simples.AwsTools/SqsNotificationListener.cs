using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
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
        private readonly Dictionary<Type, List<Action<Message>>> _handlers;
        private bool _listen = true;
        private static readonly Logger Log = LogManager.GetLogger("JustEat.Simples.NotificationStack");

        public SqsNotificationListener(SqsQueueBase queue, IMessageSerialisationRegister serialisationRegister, IMessageFootprintStore messageFootprintStore, IMessageMonitor messagingMonitor, Action<Exception> onError = null)
        {
            _queue = queue;
            _serialisationRegister = serialisationRegister;
            _messageFootprintStore = messageFootprintStore;
            _messagingMonitor = messagingMonitor;
            _onError = onError ?? (ex => { });
            _handlers = new Dictionary<Type, List<Action<Message>>>();
        }

        public void AddMessageHandler<T>(IHandler<T> handler) where T : Message
        {
            List<Action<Message>> handlers;
            if (!_handlers.TryGetValue(typeof(T), out handlers))
            {
                handlers = new List<Action<Message>>();
                _handlers.Add(typeof(T), handlers);
            }
            
            handlers.Add(DelegateAdjuster.CastArgument<Message, T>(x => RepeatCallSafe(x, handler)));
        }

        private void RepeatCallSafe<T>(T message, IHandler<T> handler) where T : Message
        {
            if (_messageFootprintStore.IsMessageReceieved(message.Id))
                return;

            handler.Handle(message);

            _messageFootprintStore.MarkMessageAsRecieved(message.Id);
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

        private void ListenLoop()
        {
            try
            {
                var sqsMessageResponse = _queue.Client.ReceiveMessage(new ReceiveMessageRequest()
                                                                          .WithQueueUrl(_queue.Url)
                                                                          .WithMaxNumberOfMessages(10)
                                                                          .WithWaitTimeSeconds(20));

                var messageCount = (sqsMessageResponse.IsSetReceiveMessageResult()) ? sqsMessageResponse.ReceiveMessageResult.Message.Count : 0;
                Log.Trace(string.Format("Polled for messages - Queue: {0}, MessageCount: {1}", _queue.QueueNamePrefix, messageCount));
                sqsMessageResponse.ReceiveMessageResult.Message.ForEach(HandleMessage);
            }
            catch (InvalidOperationException ex) { Log.Trace("Suspected no messaged in queue. Ex: {0}", ex); }
            catch (Exception ex)
            {
                Log.ErrorException("Issue in message handling loop", ex);
                _messagingMonitor.HandleException();
            }
        }

        public void HandleMessage(Amazon.SQS.Model.Message message)
        {
            Action run = () =>
            {
                try
                {
                    var messageType = JObject.Parse(message.Body)["Subject"].ToString();

                    var typedMessage = _serialisationRegister
                                .GetSerialiser(messageType)
                                .Deserialise(JObject.Parse(message.Body)["Message"].ToString());

                    if (typedMessage != null)
                    {
                        List<Action<Message>> handlers;
                        if (!_handlers.TryGetValue(typedMessage.GetType(), out handlers)) return;
                        foreach (var handler in handlers)
                        {
                            var watch = new System.Diagnostics.Stopwatch();
                            watch.Start();
                            handler(typedMessage);
                            watch.Stop();
                            Log.Trace("Handled message - MessageType: " + messageType);
                            _messagingMonitor.HandleTime(watch.ElapsedMilliseconds);
                        }
                    }

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
            };

            run.BeginInvoke(null, null);
        }
    }
}
