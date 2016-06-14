using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.Logging;
using JustSaying.Messaging.MessageSerialisation;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SnsTopicByName : SnsTopicBase
    {
        public string TopicName { get; private set; }
        private static readonly ILog Log = LogProvider.GetLogger("JustSaying");

        public SnsTopicByName(string topicName, IAmazonSimpleNotificationService client, IMessageSerialisationRegister serialisationRegister)
            : base(serialisationRegister)
        {
            TopicName = topicName;
            Client = client;
        }

        public override bool Exists()
        {
            if (string.IsNullOrWhiteSpace(Arn) == false)
                return true;

            Log.InfoFormat("Checking if topic '{0}' exists", TopicName);
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