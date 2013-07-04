using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
using Newtonsoft.Json.Linq;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using Message = JustEat.Simples.NotificationStack.Messaging.Messages.Message;

namespace JustEat.Simples.NotificationStack.AwsTools
{
    public class SqsNotificationListener : INotificationSubscriber
    {
        private readonly SqsQueueByUrl _queue;
        private readonly IMessageSerialisationRegister _serialisationRegister;
        private readonly Dictionary<Type, List<Action<Message>>> _handlers;

        public SqsNotificationListener(SqsQueueByUrl queue, IMessageSerialisationRegister serialisationRegister)
        {
            _queue = queue;
            _serialisationRegister = serialisationRegister;
            _handlers = new Dictionary<Type, List<Action<Message>>>();
        }

        public void AddMessageHandler<T>(Action<T> handler) where T : Message
        {
            List<Action<Message>> handlers;
            if (!_handlers.TryGetValue(typeof(T), out handlers))
            {
                handlers = new List<Action<Message>>();
                _handlers.Add(typeof(T), handlers);
            }
            handlers.Add(DelegateAdjuster.CastArgument<Message, T>(x => handler(x)));
        }

        public void Listen()
        {
            var messageResult = _queue.Client.ReceiveMessage(new ReceiveMessageRequest()
                                                                 .WithQueueUrl(_queue.Url)
                                                                 .WithMaxNumberOfMessages(10))
                .ReceiveMessageResult;

            foreach (var message in messageResult.Message)
            {
                try
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
                catch (InvalidOperationException) { } // Swallow no messages
            }

            //var endpoint = new JustEat.Simples.NotificationStack.Messaging.Lookups.SqsSubscribtionEndpointProvider().GetLocationEndpoint(Component.OrderEngine, PublishTopics.CustomerCommunication);
            //var queue = new AwsTools.SqsQueueByUrl(endpoint, Amazon.AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.EUWest1));
        }
    }
}
