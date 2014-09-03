using System;
using System.Linq;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialisation;

namespace JustSaying.AwsTools
{
    public class SnsTopicByArn : SnsTopicBase
    {
        public SnsTopicByArn(string topicArn, IAmazonSimpleNotificationService client, IMessageSerialisationRegister serialisationRegister)
            :base(serialisationRegister)
        {
            Arn = topicArn;
            Client = client;
        }

        public override bool Exists()
        {
            var topicCheck = Client.ListTopics(new ListTopicsRequest());
            return topicCheck.Topics.Any(x => x.TopicArn == Arn);
        }
        public bool Matches(string topicName)
        {
            return Arn
                .Substring(Arn.LastIndexOf(":", StringComparison.InvariantCulture) + 1)
                .Equals(topicName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}