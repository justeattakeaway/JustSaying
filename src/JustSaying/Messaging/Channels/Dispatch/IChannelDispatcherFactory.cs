namespace JustSaying.Messaging.Channels.Dispatch
{
    internal interface IChannelDispatcherFactory
    {
        IChannelDispatcher Create();
    }
}
