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

        public SnsTopicByName(string topicName, IAmazonSimpleNotificationService client, IMessageSerialisationRegister serialisationRegister, ILoggerFactory loggerFactory, IMessageSubjectProvider messageSubjectProvider)
            : base(serialisationRegister, loggerFactory, messageSubjectProvider)
        {
            TopicName = topicName;
            Client = client;
            _log = loggerFactory.CreateLogger("JustSaying");
        }

        public SnsTopicByName(string topicName, IAmazonSimpleNotificationService client,
            IMessageSerialisationRegister serialisationRegister,
            ILoggerFactory loggerFactory, SnsWriteConfiguration snsWriteConfiguration,
            IMessageSubjectProvider messageSubjectProvider)
            : base(serialisationRegister, loggerFactory, snsWriteConfiguration, messageSubjectProvider)
        {
            TopicName = topicName;
            Client = client;
            _log = loggerFactory.CreateLogger("JustSaying");
        }

        public async Task EnsurePolicyIsUpdatedAsync(IReadOnlyCollection<string> config)
        {
            if (config.Any())
            {
                var policy = new SnsPolicy(config);
                await policy.SaveAsync(Arn, Client).ConfigureAwait(false);
            }
        }

        public override async Task<bool> ExistsAsync()
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
                if (!await ExistsAsync().ConfigureAwait(false))
                {
                    throw new InvalidOperationException("Topic does not exist and no permission to create it!");
                }
            }

            return false;
        }
    }
}
