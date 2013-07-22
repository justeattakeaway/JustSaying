using System.Linq;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using NLog;

namespace JustEat.Simples.NotificationStack.AwsTools
{
    public abstract class SnsTopicBase
    {
        private readonly IMessageSerialisationRegister _serialisationRegister;
        public string Arn { get; protected set; }
        public AmazonSimpleNotificationService Client { get; protected set; }
        private static readonly Logger Log = LogManager.GetLogger("EventLog");

        public SnsTopicBase(IMessageSerialisationRegister serialisationRegister)
        {
            _serialisationRegister = serialisationRegister;
        }

        public abstract bool Exists();

        public bool IsSubscribed(SqsQueueBase queue)
        {
            var result = Client.ListSubscriptionsByTopic(new ListSubscriptionsByTopicRequest().WithTopicArn(Arn));
            if (result.IsSetListSubscriptionsByTopicResult())
            {
                return result.ListSubscriptionsByTopicResult.Subscriptions.Any(x => x.IsSetSubscriptionArn() && x.Endpoint == queue.Arn);
            }
            return false;
        }

        public bool Subscribe(SqsQueueBase queue)
        {
            var response = Client.Subscribe(new SubscribeRequest().WithTopicArn(Arn).WithProtocol("sqs").WithEndpoint(queue.Arn));
            if (response.IsSetSubscribeResult() && response.SubscribeResult.IsSetSubscriptionArn())
            {
                queue.AddPermission(this);
                return true;
            }
            return false;
        }

        public void Publish(Message message)
        {
            var messageToSend = _serialisationRegister.GetSerialiser(message.GetType()).Serialise(message);
            var messageType = message.GetType().Name;

            Client.Publish(new PublishRequest
                               {
                                   Subject = messageType,
                                   Message = messageToSend,
                                   TopicArn = Arn
                               });

            Log.Info("Published message: '{0}' with content {1}", messageType, messageToSend);
        }
    }
}