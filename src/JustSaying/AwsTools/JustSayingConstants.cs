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
}
