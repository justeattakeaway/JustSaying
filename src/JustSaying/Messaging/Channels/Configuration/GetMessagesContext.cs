namespace JustSaying.Messaging.Channels.Configuration
{
    public class GetMessagesContext
    {
        public int Count { get; set; }
        public string QueueName { get; set; }
        public string RegionName { get; set; }
    }
}
