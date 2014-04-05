using System;

namespace JustSaying.Stack
{
    public interface IMessagingConfig
    {
        string Component { get; set; }
        string Tenant { get; set; }
        string Environment { get; set; }
    }

    public class MessagingConfig : JustSaying.MessagingConfig, IMessagingConfig, INotificationStackConfiguration
    {
        public string Component { get; set; }
        public string Tenant { get; set; }
        public string Environment { get; set; }

        public override void Validate()
        {
            base.Validate();
            if (string.IsNullOrWhiteSpace(Environment))
                throw new ArgumentNullException("config.Environment", "Cannot have a blank entry for config.Environment");

            if (string.IsNullOrWhiteSpace(Tenant))
                throw new ArgumentNullException("config.Tenant", "Cannot have a blank entry for config.Tenant");

            if (string.IsNullOrWhiteSpace(Component))
                throw new ArgumentNullException("config.Component", "Cannot have a blank entry for config.Component");
        }
    }
}