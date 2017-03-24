using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatchLogs.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging.MessageSerialisation;
using NLog;

namespace JustSaying.AwsTools.MessageHandling
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
        }

        public override bool Exists()
        {
            if (!string.IsNullOrWhiteSpace(Arn))
            {
                return true;
            }

            Log.Info("Checking if topic '{0}' exists", TopicName);
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
            try
            {
                var response = Client.CreateTopic(new CreateTopicRequest(TopicName));

                if (!string.IsNullOrEmpty(response.TopicArn))
                {
                    Arn = response.TopicArn;
                    Log.Info("Topic: {0} on Arn: {1}", TopicName, Arn);
                    return true;
                }
                Log.Info("Failed to create Topic: {0}", TopicName);
            }
            catch (AuthorizationErrorException ex)
            {
                Log.Warn(ex, "Not authorized to create topic: {0}", TopicName);
                if (!Exists())
                {
                    throw new InvalidOperationException("Topic does not exist and no permission to create it!");
                }
            }
            
            return false;
        }

        public void EnsurePolicyIsUpdated(IReadOnlyCollection<string> config)
        {
            if (config.Any())
            {
                var policy = new SnsPolicy(config);
                policy.Save(Arn, Client);
            }
        }
    }
}
