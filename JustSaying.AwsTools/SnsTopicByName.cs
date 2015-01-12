using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging.MessageSerialisation;
using NLog;

namespace JustSaying.AwsTools
{
    public class SnsTopicByName : SnsTopicBase
    {
        public string TopicName { get; private set; }
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        public SnsTopicByName(string topicName, IAmazonSimpleNotificationService client, IMessageSerialisationRegister serialisationRegister)
            : base(serialisationRegister)
        {
            TopicName = topicName;
            Client = client;
            Exists();
        }

        public override bool Exists()
        {
            var topic = Client.FindTopic(TopicName);

            if (topic != null)
            {
                Arn = topic.TopicArn;
                return true;
            }
            
            return false;
        }

        public bool Create()
        {
            var response = Client.CreateTopic(new CreateTopicRequest(TopicName));
            if (!string.IsNullOrEmpty(response.TopicArn))
            {    
                Arn = response.TopicArn;
                Log.Info(string.Format("Created Topic: {0} on Arn: {1}", TopicName, Arn));
                return true;
            }
            Log.Info(string.Format("Failed to create Topic: {0}", TopicName));
            return false;
        }
    }
}