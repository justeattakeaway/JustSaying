using System.Linq;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using SimplesNotificationStack.Messaging.Messages;

namespace JustEat.AwsTools
{
    public abstract class SnsTopicBase
    {
        public string Arn { get; protected set; }
        public AmazonSimpleNotificationService Client { get; protected set; }

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
                                   Subject = message.GetType().ToString(),
                                   // Message = JsonConvert.SerializeObject(new NewOrder { CustomerId = i }),
                                   TopicArn = Arn
                               });
        }
    }
}