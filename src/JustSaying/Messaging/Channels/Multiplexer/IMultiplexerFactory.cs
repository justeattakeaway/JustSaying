namespace JustSaying.Messaging.Channels.Multiplexer
{
    internal interface IMultiplexerFactory
    {
        IMultiplexer Create(int channelCapacity);
    }
}
