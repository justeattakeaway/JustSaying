using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
using JustEat.AwsTools;
using Newtonsoft.Json.Linq;
using SimplesNotificationStack.Messaging;
using SimplesNotificationStack.Messaging.MessageHandling;
using SimplesNotificationStack.Messaging.MessageSerialisation;
using Message = SimplesNotificationStack.Messaging.Messages.Message;

namespace SimplesNotificationStack.AwsTools
{
    public class SqsNotificationListener : INotificationSubscriber
    {
        private readonly SqsQueueByUrl _queue;
        private readonly IMessageSerialisationRegister _serialisationRegister;
        private readonly Dictionary<Type, IHandler<Message>> _handlers;

        public SqsNotificationListener(SqsQueueByUrl queue, IMessageSerialisationRegister serialisationRegister)
        {
            _queue = queue;
            _serialisationRegister = serialisationRegister;
            _handlers = new Dictionary<Type, IHandler<Message>>();
        }

        public void AddMessageHandler(IHandler<Message> handler)
        {
            _handlers.Add(handler.HandlesMessageType, handler);
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
                        .Deserialised(JObject.Parse(message.Body)["Message"].ToString());

                    if (typedMessage != null)
                        _handlers[typedMessage.GetType()].Handle(typedMessage);

                    _queue.Client.DeleteMessage(new DeleteMessageRequest().WithQueueUrl(_queue.Url).WithReceiptHandle(message.ReceiptHandle));
                }
                catch (InvalidOperationException) { } // Swallow no messages
            }

            //var endpoint = new SimplesNotificationStack.Messaging.Lookups.SqsSubscribtionEndpointProvider().GetLocationEndpoint(Component.OrderEngine, PublishTopics.CustomerCommunication);
            //var queue = new AwsTools.SqsQueueByUrl(endpoint, Amazon.AWSClientFactory.CreateAmazonSQSClient(RegionEndpoint.EUWest1));
        }
    }
}
