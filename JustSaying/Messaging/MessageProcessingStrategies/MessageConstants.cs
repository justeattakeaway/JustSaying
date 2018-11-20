namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public static class MessageConstants
    {
        public static int MaxAmazonMessageCap => 10;
        public static int ParallelHandlerExecutionPerCore => 8;
    }
}
