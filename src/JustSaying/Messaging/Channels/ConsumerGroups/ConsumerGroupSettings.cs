namespace JustSaying.Messaging.Channels.ConsumerGroups
{
    public class ConsumerGroupSettings
    {
        public ConsumerGroupSettings(int bufferSize, int consumerCount, int multiplexerCapcity, int prefetch)
        {
            BufferSize = bufferSize;
            ConsumerCount = consumerCount;
            MultiplexerCapacity = multiplexerCapcity;
            Prefetch = prefetch;
        }

        public int ConsumerCount { get; }
        public int BufferSize { get; }
        public int MultiplexerCapacity { get; }
        public int Prefetch { get; }
    }
}
