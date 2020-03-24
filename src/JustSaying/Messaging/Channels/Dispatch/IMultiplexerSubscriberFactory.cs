namespace JustSaying.Messaging.Channels.Dispatch
{
    internal interface IMultiplexerSubscriberFactory
    {
        IMultiplexerSubscriber Create();
    }
}
