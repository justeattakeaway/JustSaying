using System.Linq;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.MessageSerialisation;

namespace JustEat.Simples.NotificationStack.AwsTools
{
    public class SnsTopicByArn : SnsTopicBase, IMessagePublisher
    {
        public SnsTopicByArn(string topicArn, AmazonSimpleNotificationService client, IMessageSerialisationRegister serialisationRegister)
            :base(serialisationRegister)
        {
            Arn = topicArn;
            Client = client;
        }

        public override bool Exists()
        {
            var topicCheck = Client.ListTopics(new ListTopicsRequest());
            return topicCheck.IsSetListTopicsResult() && topicCheck.ListTopicsResult.Topics.Any(x => x.TopicArn == Arn);
        }
    }
}