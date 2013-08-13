using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
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
        private readonly Dictionary<Type, List<Action<Message>>> _handlers;
        private bool _listen = true;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public SqsNotificationListener(SqsQueueBase queue, IMessageSerialisationRegister serialisationRegister, IMessageFootprintStore messageFootprintStore)
        {
            _queue = queue;
            _serialisationRegister = serialisationRegister;
            _messageFootprintStore = messageFootprintStore;
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
            Action run = () => { while (_listen) { ListenLoop(); } };
            run.BeginInvoke(null, null);
        }

        public void StopListening()
        {
            _listen = false;
        }

        private void ListenLoop()
        {
            try
            {
                var sqsMessageResponse = _queue.Client.ReceiveMessage(new ReceiveMessageRequest()
                                                                          .WithQueueUrl(_queue.Url)
                                                                          .WithMaxNumberOfMessages(10)
                                                                          .WithWaitTimeSeconds(20));

                foreach (var message in sqsMessageResponse.ReceiveMessageResult.Message)
                {
                    var typedMessage = _serialisationRegister
                        .GetSerialiser(JObject.Parse(message.Body)["Subject"].ToString())
                        .Deserialise(JObject.Parse(message.Body)["Message"].ToString());

                    if (typedMessage != null)
                    {
                        List<Action<Message>> handlers;
                        if (!_handlers.TryGetValue(typedMessage.GetType(), out handlers)) return;
                        foreach (var handler in handlers)
                        {
                            handler(typedMessage);
                        }
                    }

                    _queue.Client.DeleteMessage(new DeleteMessageRequest().WithQueueUrl(_queue.Url).WithReceiptHandle(message.ReceiptHandle));
                }
            }
            catch (InvalidOperationException ex) { Log.Trace("Suspected no messaged in queue. Ex: {0}", ex); }
            catch (Exception ex) { Log.ErrorException("Issue in message handling loop", ex); }
        }

        
    }
}
