using System.Linq;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using JustSaying.Messaging;
using JustSaying.Messaging.Extensions;
using JustSaying.Messaging.MessageSerialisation;
using NLog;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools
{
    public abstract class SnsTopicBase : IMessagePublisher
    {
        private readonly IMessageSerialisationRegister _serialisationRegister; // ToDo: Grrr...why is this here even. GET OUT!
        public string Arn { get; protected set; }
        public IAmazonSimpleNotificationService Client { get; protected set; }
        private static readonly Logger EventLog = LogManager.GetLogger("EventLog");
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        public SnsTopicBase(IMessageSerialisationRegister serialisationRegister)
        {
            _serialisationRegister = serialisationRegister;
        }

        public abstract bool Exists();

        public bool IsSubscribed(SqsQueueBase queue)
        {
            var result = Client.ListSubscriptionsByTopic(new ListSubscriptionsByTopicRequest(Arn));
            
            return result.Subscriptions.Any(x => !string.IsNullOrEmpty(x.SubscriptionArn) && x.Endpoint == queue.Arn);
        }

        public bool Subscribe(IAmazonSQS amazonSQSClient, SqsQueueBase queue)
        {
            var subscriptionArn = Client.SubscribeQueue(Arn, amazonSQSClient, queue.Url);
            if (!string.IsNullOrEmpty(subscriptionArn))
            {
                return true;
            }

            Log.Info(string.Format("Failed to subscribe Queue to Topic: {0}, Topic: {1}", queue.Arn, Arn));
            return false;
        }

        public void Publish(Message message)
        {
            var messageToSend = _serialisationRegister.GeTypeSerialiser(message.GetType()).Serialiser.Serialise(message);
            var messageType = message.GetType().ToKey();

            Client.Publish(new PublishRequest
                               {
                                   Subject = messageType,
                                   Message = messageToSend,
                                   TopicArn = Arn
                               });

            EventLog.Info("Published message: '{0}' with content {1}", messageType, messageToSend);
        }
    }
}