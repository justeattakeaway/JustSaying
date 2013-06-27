using System.Linq;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using SimplesNotificationStack.Messaging;
using SimplesNotificationStack.Messaging.Messages;

namespace JustEat.AwsTools
{
    public class SnsTopic : IMessagePublisher
    {
        public string TopicName { get; private set; }
        public string Arn { get; private set; }
        public AmazonSimpleNotificationService Client { get; private set; }

        public SnsTopic(string topicName, AmazonSimpleNotificationService client)
        {
            TopicName = topicName;
            Client = client;
            Exists();
        }

        public bool Exists()
        {
            var topicCheck = Client.ListTopics(new ListTopicsRequest());
            if (topicCheck.IsSetListTopicsResult())
            {
                var topic = topicCheck.ListTopicsResult.Topics.FirstOrDefault(x => x.TopicArn.Contains(TopicName));
                if (topic != null)
                {
                    Arn = topic.TopicArn;
                    return true;
                }
            }

            return false;
        }

        public bool Create()
        {
            var response = Client.CreateTopic(new CreateTopicRequest().WithName(TopicName));
            if (response.IsSetCreateTopicResult() && response.CreateTopicResult.TopicArn != null)
            {    
                Arn = response.CreateTopicResult.TopicArn;
                return true;
            }
            return false;
        }

        public void Delete()
        {
            if (!Exists())
                return;

            var response = Client.DeleteTopic(new DeleteTopicRequest().WithTopicArn(Arn));
            Arn = string.Empty;
        }

        public bool IsSubscribed(SqsQueue queue)
        {
            var result = Client.ListSubscriptionsByTopic(new ListSubscriptionsByTopicRequest().WithTopicArn(Arn));
            if (result.IsSetListSubscriptionsByTopicResult())
            {
                return result.ListSubscriptionsByTopicResult.Subscriptions.Any(x => x.IsSetSubscriptionArn() && x.Endpoint == queue.Arn);
            }
            return false;
        }

        public bool Subscribe(SqsQueue queue)
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