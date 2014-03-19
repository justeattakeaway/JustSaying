using JustEat.Simples.NotificationStack.Messaging;

namespace JustEat.Simples.NotificationStack.Stack
{
    public interface IMessagingConfig
    {
        string Component { get; set; }
        string Tenant { get; set; }
        string Environment { get; set; }
    }

    public class MessagingConfig : SimpleMessageMule.MessagingConfig, IMessagingConfig, INotificationStackConfiguration, SimpleMessageMule.IMessagingConfig
    {
        public string Component { get; set; }
        public string Tenant { get; set; }
        public string Environment { get; set; }

    }
}