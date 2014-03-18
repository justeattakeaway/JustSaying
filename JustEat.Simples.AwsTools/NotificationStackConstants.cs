namespace JustEat.Simples.NotificationStack.AwsTools
{
    public static class NotificationStackConstants
    {
        public const string ATTRIBUTE_REDRIVE_POLICY = "RedrivePolicy";
        public const int DEFAULT_CREATE_REATTEMPT = 0;
        public const int DEFAULT_VISIBILITY_TIMEOUT = 30;
        public const int DEFAULT_HANDLER_RETRY_COUNT = 5;
        public const int DEFAULT_PUBLISHER_RETRY_COUNT = 3;
        public const int DEFAULT_PUBLISHER_RETRY_INTERVAL = 100;//100 milliseconds
        public const int MINIMUM_RETENTION_PERIOD = 60;         //1 minute
        public const int MAXIMUM_RETENTION_PERIOD = 1209600;    //14 days

    }
}