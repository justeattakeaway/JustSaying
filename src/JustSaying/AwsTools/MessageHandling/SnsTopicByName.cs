using System.Net;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.Interrogation;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling;

[Obsolete("SnsTopicByName is not intended for general usage and may be removed in a future major release")]
public sealed class SnsTopicByName(
    string topicName,
    IAmazonSimpleNotificationService client,
    ILoggerFactory loggerFactory) : IInterrogable
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("JustSaying");

    public string TopicName { get; } = topicName;
    public string Arn { get; private set; }
    internal ServerSideEncryption ServerSideEncryption { get; set; }
    public IDictionary<string, string> Tags { get; set; }

    public async Task EnsurePolicyIsUpdatedAsync(IReadOnlyCollection<string> additionalSubscriberAccounts)
    {
        if (additionalSubscriberAccounts.Count != 0)
        {
            var policyDetails = new SnsPolicyDetails
            {
                AccountIds = additionalSubscriberAccounts,
                SourceArn = Arn
            };
            await SnsPolicy.SaveAsync(policyDetails, client).ConfigureAwait(false);
        }
    }

    public async Task ApplyTagsAsync(CancellationToken cancellationToken)
    {
        if (Tags.Count < 1)
        {
            return;
        }

        static Tag CreateTag(KeyValuePair<string, string> tag) => new() { Key = tag.Key, Value = tag.Value };

        var tagRequest = new TagResourceRequest
        {
            ResourceArn = Arn,
            Tags = Tags.Select(CreateTag).ToList()
        };

        await client.TagResourceAsync(tagRequest, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Added {TagCount} tags to topic {TopicName}", tagRequest.Tags.Count, TopicName);
    }

    public async Task<bool> ExistsAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(Arn))
        {
            return true;
        }

        _logger.LogInformation("Checking for existence of the topic '{TopicName}'.", TopicName);
        var topic = await client.FindTopicAsync(TopicName).ConfigureAwait(false);

        if (topic != null)
        {
            Arn = topic.TopicArn;
            return true;
        }

        return false;
    }

    public async Task CreateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.CreateTopicAsync(new CreateTopicRequest(TopicName), cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(response.TopicArn))
            {
                var requestId = response.ResponseMetadata.RequestId;
                _logger.LogError("Failed to create or obtain ARN for topic {TopicName}. RequestId: {RequestId}.",
                    TopicName, requestId);
                throw new InvalidOperationException($"Failed to create or obtain ARN for topic '{TopicName}'. RequestId: {requestId}.");
            }

            Arn = response.TopicArn;
            _logger.LogDebug("Created topic '{TopicName}' with ARN '{Arn}'.", TopicName, Arn);
        }
        catch (AuthorizationErrorException ex)
        {
            _logger.LogWarning(0, ex, "Not authorized to create topic '{TopicName}'.", TopicName);
            if (!await ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException(
                    $"Topic '{TopicName}' does not exist, and no permission to create it.");
            }
        }
    }

    public async Task CreateWithEncryptionAsync(ServerSideEncryption config, CancellationToken cancellationToken)
    {
        await CreateAsync(cancellationToken).ConfigureAwait(false);

        ServerSideEncryption =
            await ExtractServerSideEncryptionFromTopicAttributes().ConfigureAwait(false);

        await EnsureServerSideEncryptionIsUpdatedAsync(config).ConfigureAwait(false);
    }

    private async Task<ServerSideEncryption> ExtractServerSideEncryptionFromTopicAttributes()
    {
        var attributesResponse = await client.GetTopicAttributesAsync(Arn).ConfigureAwait(false);

        if (!attributesResponse.Attributes.TryGetValue(
                JustSayingConstants.AttributeEncryptionKeyId,
                out var encryptionKeyId))
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

            var response = await client.SetTopicAttributesAsync(request).ConfigureAwait(false);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                ServerSideEncryption = string.IsNullOrEmpty(config?.KmsMasterKeyId)
                    ? null
                    : config;
            }
            else
            {
                _logger.LogWarning(
                    "Request to set topic attribute '{TopicAttributeName}' to '{TopicAttributeValue}' failed with status code '{HttpStatusCode}'.",
                    request.AttributeName,
                    request.AttributeValue,
                    response.HttpStatusCode);
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

    public InterrogationResult Interrogate()
    {
        return new InterrogationResult(new
        {
            Arn,
            TopicName
        });
    }

}
