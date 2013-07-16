using System.Linq;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;
using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.AwsTools
{
    public abstract class SnsTopicBase
    {
        private readonly IMessageSerialisationRegister _serialisationRegister;
        public string Arn { get; protected set; }
        public AmazonSimpleNotificationService Client { get; protected set; }

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
            Client.Publish(new PublishRequest
                               {
                                   Subject = message.GetType().Name,
                                   Message = _serialisationRegister.GetSerialiser(message.GetType()).Serialise(message),
                                   TopicArn = Arn
                               });
        }
    }
}