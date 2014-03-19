namespace JustEat.Simples.NotificationStack.Stack
{
    public interface INotificationStackConfiguration : SimpleMessageMule.INotificationStackConfiguration
    {
        string Component { get; set; }
        string Tenant { get; set; }
        string Environment { get; set; }
    }
}