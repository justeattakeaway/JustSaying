namespace JustSaying.AwsTools
{
    public static class JustSayingConstants
    {
        public const string ATTRIBUTE_REDRIVE_POLICY = "RedrivePolicy";
        public const string ATTRIBUTE_ARN = "QueueArn";
        public const string ATTRIBUTE_RETENTION_PERIOD = "MessageRetentionPeriod";
        public const string ATTRIBUTE_VISIBILITY_TIMEOUT = "VisibilityTimeout";
        public const string ATTRIBUTE_DELIVERY_DELAY = "DelaySeconds";
        public const string ATTRIBUTE_POLICY = "Policy";

        /// <summary>
        /// Default visibility timeout for message in seconds
        /// </summary>
        public static int DEFAULT_VISIBILITY_TIMEOUT = 30;
        
        /// <summary>
        /// Number of times a handler will retry a message until a message 
        /// is sent to error queue
        /// </summary>
        public static int DEFAULT_HANDLER_RETRY_COUNT = 5;

        /// <summary>
        /// Number of times publisher will retry to publish a message if destination is down.
        /// </summary>
        public static int DEFAULT_PUBLISHER_RETRY_COUNT = 3;

        /// <summary>
        /// Every time a publisher is not able to deliver a message, it will 
        /// wait {interval}*{attemptCount} miliseconds before retrying,
        /// </summary>
        public static int DEFAULT_PUBLISHER_RETRY_INTERVAL = 100;//100 milliseconds
        
        /// <summary>
        /// Minimum message retention period on a queue.
        /// </summary>
        public static int MINIMUM_RETENTION_PERIOD = 60;         //1 minute

        /// <summary>
        /// Default message retention period on a queue in seconds
        /// </summary>
        public static int DEFAULT_RETENTION_PERIOD = 345600;    //4 days

        /// <summary>
        /// Maximum message retention period on a queue in seconds
        /// </summary>
        public static int MAXIMUM_RETENTION_PERIOD = 1209600;    //14 days
        
        /// <summary>
        /// Minimum delay in message delivery for SQS i nseconds. This is also the default.
        /// </summary>
        public static int MINIMUM_DELIVERY_DELAY = 0;

        /// <summary>
        /// Maximum message delivery delay for SQS in seconds
        /// </summary>
        public static int MAXIMUM_DELIVERY_DELAY = 900;          //15 minutes
    }
}