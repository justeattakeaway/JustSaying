using System;
using System.Configuration;
using JustSaying.Messaging.MessageProcessingStrategies;

namespace JustSaying.AwsTools.QueueCreation
{
    public class SqsReadConfiguration : SqsBasicConfiguration
    {
        internal string QueueName { get; set; }
        internal string Topic { get; set; }
        internal string PublishEndpoint { get; set; }

        public int? InstancePosition { get; set; }
        public int? MaxAllowedMessagesInFlight { get; set; }
        public IMessageProcessingStrategy MessageProcessingStrategy { get; set; }
        public Action<Exception> OnError { get; set; }

        public override void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(Topic))
                throw new ConfigurationErrorsException("Invalid configuration. Topic must be provided.");
            
            if (MaxAllowedMessagesInFlight.HasValue && MessageProcessingStrategy != null)
                throw new ConfigurationErrorsException("You have provided both 'maxAllowedMessagesInFlight' and 'messageProcessingStrategy' - these settings are mutually exclusive.");

            if(PublishEndpoint == null)
                throw new ConfigurationErrorsException("You must provide a value for PublishEndpoint.");
        }
    }
}