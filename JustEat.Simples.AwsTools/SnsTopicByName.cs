using System.Linq;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;

namespace JustEat.Simples.NotificationStack.AwsTools
{
    public class SnsTopicByName : SnsTopicBase, IMessagePublisher
    {
        public string TopicName { get; private set; }

        public SnsTopicByName(string topicName, AmazonSimpleNotificationService client, IMessageSerialisationRegister serialisationRegister)
            : base(serialisationRegister)
        {
            TopicName = topicName;
            Client = client;
            Exists();
        }

        public override bool Exists()
        {
            var topicCheck = Client.ListTopics(new ListTopicsRequest());
            if (topicCheck.IsSetListTopicsResult())
            {
                var topic = topicCheck.ListTopicsResult.Topics.FirstOrDefault(x => x.TopicArn.Contains(TopicName));

                while (topic == null && topicCheck.ListTopicsResult.IsSetNextToken())
                {
                    topicCheck = Client.ListTopics(new ListTopicsRequest().WithNextToken(topicCheck.ListTopicsResult.NextToken));
                    topic = topicCheck.ListTopicsResult.Topics.FirstOrDefault(x => x.TopicArn.Contains(TopicName));
                    
                    if (topic != null)
                    {
                        Arn = topic.TopicArn;
                        return true;
                    }
                }

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
    }
}