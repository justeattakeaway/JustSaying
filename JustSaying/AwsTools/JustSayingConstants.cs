using System;

namespace JustSaying.AwsTools
{
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
        /// Default visibility timeout for message in seconds
        /// </summary>
        public static int DefaultVisibilityTimeout => 30;
        
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
        /// wait {interval}*{attemptCount} miliseconds before retrying,
        /// </summary>
        public static int DefaultPublisherRetryInterval => 100;//100 milliseconds

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
        /// Minimum delay in message delivery for SQS i nseconds. This is also the default.
        /// </summary>
        public static int MinimumDeliveryDelay => 0;

        /// <summary>
        /// Maximum message delivery delay for SQS in seconds
        /// </summary>
        public static int MaximumDeliveryDelay => 900;          //15 minutes

        /// <summary>
        /// Default ID of an AWS-managed customer master key (CMK) for Amazon SQS
        /// </summary>
        public static string DefaultAttributeEncryptionKeyId => "alias/aws/sqs";

        /// <summary>
        /// Default length of time, in seconds, for which Amazon SQS can reuse a data key to encrypt/decrypt messages before calling AWS KMS again
        /// </summary>
        public static string DefaultAttributeEncryptionKeyReusePeriodSecond => "300";  //5 minutes
    }
}
