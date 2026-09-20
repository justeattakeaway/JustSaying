namespace JustSaying.AwsTools;

public static class JustSayingConstants
{
    public const string AttributeRedrivePolicy = "RedrivePolicy";
    public const string AttributeArn = "QueueArn";
    public const string AttributeRetentionPeriod = "MessageRetentionPeriod";
    public const string AttributeVisibilityTimeout = "VisibilityTimeout";
    public const string AttributeDeliveryDelay = "DelaySeconds";
    public const string AttributePolicy = "Policy";
    public const string AttributeEncryptionKeyId = "KmsMasterKeyId";
    public const string AttributeEncryptionKeyReusePeriodSecondId = "KmsDataKeyReusePeriodSeconds";
    public const string AttributeMaximumMessageSize = "MaximumMessageSize";

    /// <summary>
    /// Default visibility timeout for message
    /// </summary>
    public static TimeSpan DefaultVisibilityTimeout => TimeSpan.FromSeconds(30);

    /// <summary>
    /// Number of times a handler will retry a message until a message
    /// is sent to error queue
    /// </summary>
    public static int DefaultHandlerRetryCount => 5;

    /// <summary>
    /// Number of times publisher will retry to publish a message if destination is down.
    /// </summary>
    public static int DefaultPublisherRetryCount => 3;

    /// <summary>
    /// Every time a publisher is not able to deliver a message, it will
    /// wait {interval} * {attemptCount} before retrying,
    /// </summary>
    public static TimeSpan DefaultPublisherRetryInterval => TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Minimum message retention period on a queue.
    /// </summary>
    public static TimeSpan MinimumRetentionPeriod => TimeSpan.FromMinutes(1);

    /// <summary>
    /// Default message retention period on a queue.
    /// </summary>
    public static TimeSpan DefaultRetentionPeriod => TimeSpan.FromDays(4);

    /// <summary>
    /// Maximum message retention period on a queue.
    /// </summary>
    public static TimeSpan MaximumRetentionPeriod => TimeSpan.FromDays(14);

    /// <summary>
    /// Minimum delay in message delivery for SQS. This is also the default.
    /// </summary>
    public static TimeSpan MinimumDeliveryDelay => TimeSpan.Zero;

    /// <summary>
    /// Maximum message delivery delay for SQS
    /// </summary>
    public static TimeSpan MaximumDeliveryDelay => TimeSpan.FromMinutes(15);

    /// <summary>
    /// Default ID of an AWS-managed customer master key (CMK) for Amazon SQS
    /// </summary>
    public static string DefaultSqsAttributeEncryptionKeyId => "alias/aws/sqs";

    /// <summary>
    /// Default ID of an AWS-managed customer master key (CMK) for Amazon SNS
    /// </summary>
    public static string DefaultSnsAttributeEncryptionKeyId => "alias/aws/sns";

    /// <summary>
    /// Default length of time for which Amazon SQS can reuse a data key to encrypt/decrypt messages before calling AWS KMS again.
    /// </summary>
    public static TimeSpan DefaultAttributeEncryptionKeyReusePeriod => TimeSpan.FromMinutes(5);

    /// <summary>
    /// The maximum SNS batch size.
    /// </summary>
    /// <remarks>
    /// The default value is 10. See https://docs.aws.amazon.com/sns/latest/dg/sns-batch-api-actions.html.
    /// </remarks>
    public static int MaximumSnsBatchSize => 10;

    /// <summary>
    /// The maximum SQS batch size.
    /// </summary>
    /// <remarks>
    /// The default value is 10. See https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/quotas-messages.html.
    /// </remarks>
    public static int MaximumSqsBatchSize => 10;

    /// <summary>
    /// The maximum size, in bytes, of a message published to an SNS topic that has not had the
    /// <c>MaximumMessageSize</c> topic attribute set.
    /// </summary>
    /// <remarks>
    /// The default value is 262,144 bytes (256 KiB). A topic can be configured to accept payloads of up to
    /// <see cref="MaximumSnsMessageSize"/> by setting <see cref="QueueCreation.SnsWriteConfiguration.MaximumMessageSize"/>.
    /// See https://docs.aws.amazon.com/sns/latest/dg/large-message-payloads.html.
    /// </remarks>
    public static int DefaultSnsMaximumMessageSize => 256 * 1024;

    /// <summary>
    /// The largest value the SNS <c>MaximumMessageSize</c> topic attribute may be set to, in bytes.
    /// </summary>
    /// <remarks>
    /// The value is 1,048,576 bytes (1 MiB).
    /// </remarks>
    public static int MaximumSnsMessageSize => 1024 * 1024;

    /// <summary>
    /// The smallest value the SNS <c>MaximumMessageSize</c> topic attribute may be set to, in bytes.
    /// </summary>
    /// <remarks>
    /// The value is 1,024 bytes (1 KiB).
    /// </remarks>
    public static int MinimumSnsMessageSize => 1024;

    /// <summary>
    /// The maximum size, in bytes, of a message sent to an SQS queue that has not had the
    /// <c>MaximumMessageSize</c> queue attribute set.
    /// </summary>
    /// <remarks>
    /// The value is 1,048,576 bytes (1 MiB), which has been the SQS default since August 2025. A queue can
    /// still be configured with a lower limit, see <see cref="QueueCreation.SqsWriteConfiguration.MaximumMessageSize"/>.
    /// See https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/quotas-messages.html.
    /// </remarks>
    public static int DefaultSqsMaximumMessageSize => 1024 * 1024;

    /// <summary>
    /// The largest value the SQS <c>MaximumMessageSize</c> queue attribute may be set to, in bytes.
    /// </summary>
    /// <remarks>
    /// The value is 1,048,576 bytes (1 MiB).
    /// </remarks>
    public static int MaximumSqsMessageSize => 1024 * 1024;

    /// <summary>
    /// The maximum combined size, in bytes, of all the messages in a single SQS batch request.
    /// </summary>
    /// <remarks>
    /// The value is 1,048,576 bytes (1 MiB), and is not affected by the queue's <c>MaximumMessageSize</c> attribute.
    /// </remarks>
    public static int MaximumSqsBatchPayloadSize => 1024 * 1024;

    /// <summary>
    /// The smallest value the SQS <c>MaximumMessageSize</c> queue attribute may be set to, in bytes.
    /// </summary>
    /// <remarks>
    /// The value is 1,024 bytes (1 KiB).
    /// </remarks>
    public static int MinimumSqsMessageSize => 1024;

    /// <summary>
    /// The amount of headroom, in bytes, left below a destination's maximum message size when deriving
    /// a default compression threshold.
    /// </summary>
    public static int DefaultCompressionHeadroom => 2 * 1024;
}
