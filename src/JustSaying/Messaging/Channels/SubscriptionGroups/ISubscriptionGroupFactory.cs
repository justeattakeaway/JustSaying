namespace JustSaying.Messaging.Channels.SubscriptionGroups
{
    internal interface ISubscriptionGroupFactory
    {
        ISubscriptionGroup Create(SubscriptionGroupSettingsBuilder settingsBuilder);
    }
}
