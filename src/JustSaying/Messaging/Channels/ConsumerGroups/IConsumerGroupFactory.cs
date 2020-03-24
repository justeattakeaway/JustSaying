namespace JustSaying.Messaging.Channels.ConsumerGroups
{
    internal interface IConsumerGroupFactory
    {
        IConsumerGroup Create(ConsumerGroupSettingsBuilder settingsBuilder);
    }
}
