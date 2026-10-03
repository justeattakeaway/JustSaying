using System.Net;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Extensions;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools;

[Obsolete("SqsQueueBase and related classes are not intended for general usage and may be removed in a future major release")]
public class ErrorQueue(
    RegionEndpoint region,
    string sourceQueueName,
    IAmazonSQS client,
    ILoggerFactory loggerFactory) : SqsQueueByNameBase(region, sourceQueueName + "_error", client, loggerFactory)
{
    protected override Dictionary<string, string> GetCreateQueueAttributes(SqsBasicConfiguration queueConfig)
    {
        var attributes = new Dictionary<string, string>
        {
            { SQSConstants.ATTRIBUTE_MESSAGE_RETENTION_PERIOD, queueConfig.ErrorQueueRetentionPeriod.AsSecondsString() },
            { SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT, JustSayingConstants.DefaultVisibilityTimeout.AsSecondsString() },
        };

        AddEncryptionAttributes(attributes, queueConfig);

        return attributes;
    }

    public override async Task UpdateQueueAttributeAsync(SqsBasicConfiguration queueConfig, CancellationToken cancellationToken)
    {
        if (!QueueNeedsUpdating(queueConfig))
        {
            return;
        }

        var attributes = new Dictionary<string, string>
        {
            {
                JustSayingConstants.AttributeRetentionPeriod, queueConfig.ErrorQueueRetentionPeriod.AsSecondsString()
            }
        };

        AddEncryptionAttributes(attributes, queueConfig);

        var request = new SetQueueAttributesRequest
        {
            QueueUrl = Uri.AbsoluteUri,
            Attributes = attributes
        };

        var response = await Client.SetQueueAttributesAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.HttpStatusCode == HttpStatusCode.OK)
        {
            MessageRetentionPeriod = queueConfig.ErrorQueueRetentionPeriod;

            if (queueConfig.ServerSideEncryption != null)
            {
                ServerSideEncryption = queueConfig.ServerSideEncryption;
            }
        }
    }

    protected override bool QueueNeedsUpdating(SqsBasicConfiguration queueConfig)
        => MessageRetentionPeriod != queueConfig.ErrorQueueRetentionPeriod
           || QueueNeedsUpdatingBecauseOfEncryption(queueConfig);

    // The error queue holds the same messages as its source queue, so it is encrypted the same way.
    private static void AddEncryptionAttributes(Dictionary<string, string> attributes, SqsBasicConfiguration queueConfig)
    {
        if (queueConfig.ServerSideEncryption != null)
        {
            attributes.Add(JustSayingConstants.AttributeEncryptionKeyId, queueConfig.ServerSideEncryption.KmsMasterKeyId);
            attributes.Add(JustSayingConstants.AttributeEncryptionKeyReusePeriodSecondId, queueConfig.ServerSideEncryption.KmsDataKeyReusePeriod.AsSecondsString());
        }
    }
}
