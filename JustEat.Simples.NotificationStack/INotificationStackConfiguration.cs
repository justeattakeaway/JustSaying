namespace JustSaying.Stack
{
    public interface INotificationStackConfiguration : IPublishConfiguration
    {
        string Component { get; set; }
        string Tenant { get; set; }
        string Environment { get; set; }
    }
}   