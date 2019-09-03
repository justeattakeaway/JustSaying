using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SnsTopicByName : SnsTopicBase
    {
        public string TopicName { get; }
        private readonly ILogger _log;

        public SnsTopicByName(
            string topicName,
            IAmazonSimpleNotificationService client,
            IMessageSerializationRegister serializationRegister,
            ILoggerFactory loggerFactory,
            IMessageSubjectProvider messageSubjectProvider)
            : base(serializationRegister, loggerFactory, messageSubjectProvider)
        {
            TopicName = topicName;
            Client = client;
            _log = loggerFactory.CreateLogger("JustSaying");
        }

        public SnsTopicByName(
            string topicName,
            IAmazonSimpleNotificationService client,
            IMessageSerializationRegister serializationRegister,
            ILoggerFactory loggerFactory,
            SnsWriteConfiguration snsWriteConfiguration,
            IMessageSubjectProvider messageSubjectProvider)
            : base(serializationRegister, loggerFactory, snsWriteConfiguration, messageSubjectProvider)
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

            _log.LogInformation("Checking for existence of the topic '{TopicName}'.", TopicName);
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
                var response = await Client.CreateTopicAsync(new CreateTopicRequest(TopicName))
                    .ConfigureAwait(false);

                if (!string.IsNullOrEmpty(response.TopicArn))
                {
                    Arn = response.TopicArn;
                    _log.LogInformation("Created topic '{TopicName}' on ARN '{Arn}'.", TopicName, Arn);
                    return true;
                }
                _log.LogWarning("Failed to create topic '{TopicName}'.", TopicName);
            }
            catch (AuthorizationErrorException ex)
            {
                _log.LogWarning(0, ex, "Not authorized to create topic '{TopicName}'.", TopicName);
                if (!await ExistsAsync().ConfigureAwait(false))
                {
                    throw new InvalidOperationException($"Topic '{TopicName}' does not exist, and no permission to create it.");
                }
            }

            return false;
        }

        public async Task<bool> CreateWithEncryptionAsync(ServerSideEncryption config)
        {
            var created = await CreateAsync().ConfigureAwait(false);

            ServerSideEncryption = await ExtractServerSideEncryptionFromTopicAttributes().ConfigureAwait(false);

            await EnsureServerSideEncryptionIsUpdatedAsync(config).ConfigureAwait(false);

            return created;
        }

        private async Task<ServerSideEncryption> ExtractServerSideEncryptionFromTopicAttributes()
        {
            var attributesResponse = await Client.GetTopicAttributesAsync(Arn).ConfigureAwait(false);
            
            if (!attributesResponse.Attributes.TryGetValue(JustSayingConstants.AttributeEncryptionKeyId, out var encryptionKeyId))
            {
                return null;
            }

            return new ServerSideEncryption
            {
                KmsMasterKeyId = encryptionKeyId
            };
        }

        private async Task EnsureServerSideEncryptionIsUpdatedAsync(ServerSideEncryption config)
        {
            if (ServerSideEncryptionNeedsUpdating(config))
            {
                var request = new SetTopicAttributesRequest
                {
                    TopicArn = Arn,
                    AttributeName = JustSayingConstants.AttributeEncryptionKeyId,
                    AttributeValue = config?.KmsMasterKeyId ?? string.Empty
                };

                var response = await Client.SetTopicAttributesAsync(request).ConfigureAwait(false);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    ServerSideEncryption = string.IsNullOrEmpty(config?.KmsMasterKeyId)
                        ? null
                        : config;
                }
                else
                {
                    _log.LogWarning($"Request to set topic attribute {request.AttributeName} to {request.AttributeValue} failed with status code {response.HttpStatusCode}.");
                }
            }
        }

        private bool ServerSideEncryptionNeedsUpdating(ServerSideEncryption config)
        {
            if (ServerSideEncryption == config)
            {
                return false;
            }

            if (ServerSideEncryption != null && config != null)
            {
                return ServerSideEncryption.KmsMasterKeyId != config.KmsMasterKeyId;
            }

            return true;
        }
    }
}
