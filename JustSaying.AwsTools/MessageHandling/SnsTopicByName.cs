using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;

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

        public SnsTopicByName(Topic topic, IAmazonSimpleNotificationService client, IMessageSerialisationRegister serialisationRegister, ILoggerFactory loggerFactory)
            : base(serialisationRegister, loggerFactory)
        {
            TopicName = ExtractTopicName(topic.TopicArn);
            Client = client;
            _log = loggerFactory.CreateLogger("JustSaying");
            base.Arn = topic.TopicArn;
        }

        public static string ExtractTopicName(string topicArn)
        {
            if (string.IsNullOrWhiteSpace(topicArn))
            {
                throw new ArgumentNullException(nameof(topicArn));
            }

            var index = topicArn.LastIndexOf(":", StringComparison.OrdinalIgnoreCase);
            if (index < 0 || topicArn.Length <= index + 1)
            {
                throw new ArgumentException("Invalid topic ARN");
            }
            return topicArn.Substring(index + 1);
        }

        public override async Task<bool> ExistsAsync()
        {
            if (! string.IsNullOrWhiteSpace(Arn))
            {
                return true;
            }

            _log.LogInformation($"Checking if topic '{TopicName}' exists");
            var topic = await Client.FindTopicAsync(TopicName).ConfigureAwait(false); 

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
            var response = await Client.CreateTopicAsync(new CreateTopicRequest(TopicName)).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(response?.TopicArn))
            {
                Arn = response.TopicArn;
                _log.LogInformation($"Created Topic: {TopicName} on Arn: {Arn}");
                return true;
            }

            _log.LogInformation($"Failed to create Topic: {TopicName}");
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
