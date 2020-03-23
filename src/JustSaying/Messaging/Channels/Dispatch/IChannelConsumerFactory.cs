namespace JustSaying.Messaging.Channels.Dispatch
{
    internal interface IChannelConsumerFactory
    {
        IChannelConsumer Create();
    }
}
