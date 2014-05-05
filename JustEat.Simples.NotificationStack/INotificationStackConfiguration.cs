namespace JustSaying.Stack
{
    public interface INotificationStackConfiguration : IPublishConfiguration
    {
        new string Region { get; set; }
        string Component { get; set; }
        string Tenant { get; set; }
        string Environment { get; set; }
    }
}   