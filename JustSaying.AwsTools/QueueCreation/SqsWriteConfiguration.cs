using System.Configuration;

namespace JustSaying.AwsTools.QueueCreation
{
    public class SqsWriteConfiguration : SqsBasicConfiguration
    {
        internal string QueueName { get; set; }

        public override void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(QueueName))
                throw new ConfigurationErrorsException("Invalid configuration. QueueName must be provided.");
        }
    }
}