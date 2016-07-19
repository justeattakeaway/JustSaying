using System.Linq;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialisation;
using NLog;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling
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

        public bool Subscribe(IAmazonSQS amazonSqsClient, SqsQueueBase queue)
        {
            var subscriptionResponse = Client.Subscribe(Arn, "sqs", queue.Arn);
            
            if (!string.IsNullOrEmpty(subscriptionResponse.SubscriptionArn))
            {
                return true;
            }

            Log.Info($"Failed to subscribe Queue to Topic: {queue.Arn}, Topic: {Arn}");
            return false;
        }

        public void Publish(Message message)
        {
            var messageToSend = _serialisationRegister.Serialise(message, serializeForSnsPublishing:true);
            var messageType = message.GetType().Name;

            Client.Publish(new PublishRequest
                {
                    Subject = messageType,
                    Message = messageToSend,
                    TopicArn = Arn
                });

            EventLog.Info($"Published message: '{messageType}' with content {messageToSend}");
        }
    }
}