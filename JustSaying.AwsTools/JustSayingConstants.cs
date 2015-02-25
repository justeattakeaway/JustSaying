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
        public const int DEFAULT_CREATE_REATTEMPT = 0;
        public const int DEFAULT_VISIBILITY_TIMEOUT = 30;
        public const int DEFAULT_HANDLER_RETRY_COUNT = 5;
        public const int DEFAULT_PUBLISHER_RETRY_COUNT = 3;
        public const int DEFAULT_PUBLISHER_RETRY_INTERVAL = 100;//100 milliseconds
        public const int MINIMUM_RETENTION_PERIOD = 60;         //1 minute
        public const int DEFAULT_RETENTION_PERIOD = 60 * 10;    //10 minutes
        public const int MAXIMUM_RETENTION_PERIOD = 1209600;    //14 days
        public const int MINIMUM_DELIVERY_DELAY = 0;
        public const int MAXIMUM_DELIVERY_DELAY = 900;          //15 minutes
    }
}