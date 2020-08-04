namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public static class MessageDefaults
    {
        public static int MaxAmazonMessageCap => 10;
        public static int ParallelHandlerExecutionPerCore => 4;
    }
}
