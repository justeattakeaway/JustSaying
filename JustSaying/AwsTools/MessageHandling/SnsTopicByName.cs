using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialisation;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SnsTopicByName : SnsTopicBase
    {
        public string TopicName { get; }
        private readonly ILogger _log;

        public SnsTopicByName(string topicName, IAmazonSimpleNotificationService client, IMessageSerialisationRegister serialisationRegister, ILoggerFactory loggerFactory)
            : base(serialisationRegister, loggerFactory)
        {
            TopicName = topicName;
            Client = client;
            _log = loggerFactory.CreateLogger("JustSaying");
        }

        public SnsTopicByName(string topicName, IAmazonSimpleNotificationService client,
            IMessageSerialisationRegister serialisationRegister, IMessageResponseLogger messageResponseLogger,
            ILoggerFactory loggerFactory, SnsWriteConfiguration snsWriteConfiguration)
            : base(serialisationRegister, messageResponseLogger, loggerFactory, snsWriteConfiguration)
        {
            TopicName = topicName;
            Client = client;
            _log = loggerFactory.CreateLogger("JustSaying");
        }

        public void EnsurePolicyIsUpdated(IReadOnlyCollection<string> config)
        {
            if (config.Any())
            {
                var policy = new SnsPolicy(config);
                policy.Save(Arn, Client);
            }
        }

        protected override async Task<bool> ExistsAsync()
        {
            if (!string.IsNullOrWhiteSpace(Arn))
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

        public bool Create() => CreateAsync().GetAwaiter().GetResult();

        public async Task<bool> CreateAsync()
        {
            try
            {
                var response = await Client.CreateTopicAsync(new CreateTopicRequest(TopicName)).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(response.TopicArn))
                {
                    Arn = response.TopicArn;
                    _log.LogInformation($"Created Topic: {TopicName} on Arn: {Arn}");
                    return true;
                }
                _log.LogInformation($"Failed to create Topic: {TopicName}");
            }
            catch (AuthorizationErrorException ex)
            {
                _log.LogWarning(0, ex, $"Not authorized to create topic: {TopicName}");
                if (!Exists())
                {
                    throw new InvalidOperationException("Topic does not exist and no permission to create it!");
                }
            }

            return false;
        }
    }
}
