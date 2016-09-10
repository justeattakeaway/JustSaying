using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            if (! string.IsNullOrWhiteSpace(Arn))
            {
                return true;
            }

            Log.Info($"Checking if topic '{TopicName}' exists");
            var topic = Client.FindTopicAsync(TopicName)
                .GetAwaiter().GetResult();

            if (topic != null)
            {
                Arn = topic.TopicArn;
                return true;
            }

            return false;
        }

        public bool Create()
        {
            return CreateAsync()
                .GetAwaiter().GetResult();
        }

        private async Task<bool> CreateAsync()
        {
            var response = await Client.CreateTopicAsync(new CreateTopicRequest(TopicName));

            if (!string.IsNullOrEmpty(response?.TopicArn))
            {
                Arn = response.TopicArn;
                Log.Info($"Created Topic: {TopicName} on Arn: {Arn}");
                return true;
            }

            Log.Info($"Failed to create Topic: {TopicName}");
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
