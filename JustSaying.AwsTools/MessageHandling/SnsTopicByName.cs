using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;
using JustSaying.Logging;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SnsTopicByName : SnsTopicBase
    {
        public string TopicName { get; private set; }
        private readonly ILogger _log;

        public SnsTopicByName(string topicName, IAmazonSimpleNotificationService client, IMessageSerialisationRegister serialisationRegister, ILoggerFactory loggerFactory)
            : base(serialisationRegister, loggerFactory)
        {
            TopicName = topicName;
            Client = client;
            _log = loggerFactory.CreateLogger("JustSaying");
        }

        public override async Task<bool> ExistsAsync()
        {
            if (! string.IsNullOrWhiteSpace(Arn))
            {
                return true;
            }

            _log.Info($"Checking if topic '{TopicName}' exists");
            var topic = await Client.FindTopicAsync(TopicName);

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

        public async Task<bool> CreateAsync()
        {
            var response = await Client.CreateTopicAsync(new CreateTopicRequest(TopicName));

            if (!string.IsNullOrEmpty(response?.TopicArn))
            {
                Arn = response.TopicArn;
                _log.Info($"Created Topic: {TopicName} on Arn: {Arn}");
                return true;
            }

            _log.Info($"Failed to create Topic: {TopicName}");
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
