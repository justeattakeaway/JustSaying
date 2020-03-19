namespace JustSaying.Messaging.Channels.Factory
{
    internal interface IConsumerBusFactory
    {
        IConsumerBus Create(string groupName);
    }
}
