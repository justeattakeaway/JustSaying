namespace JustSaying.Messaging.Channels
{
    public class ConcurrencyGroupSettings
    {
        public ConcurrencyGroupSettings(int consumerCount)
        {
            ConsumerCount = consumerCount;
        }

        public int ConsumerCount { get; }
    }
}
