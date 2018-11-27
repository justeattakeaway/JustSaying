using System;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for configuring instances of <see cref="SqsWriteConfiguration"/>. This class cannot be inherited.
    /// </summary>
    public sealed class SqsWriteConfigurationBuilder : SqsConfigurationBuilder<SqsWriteConfiguration, SqsWriteConfigurationBuilder>
    {
        /// <inheritdoc />
        protected override SqsWriteConfigurationBuilder Self => this;

        /// <summary>
        /// Gets or sets the queue name to use.
        /// </summary>
        private string QueueName { get; set; }

        /// <summary>
        /// Configures the queue name to use.
        /// </summary>
        /// <param name="name">The value to use for the message retention.</param>
        /// <returns>
        /// The current <see cref="SqsWriteConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public SqsWriteConfigurationBuilder WithQueueName(string name)
        {
            QueueName = name ?? throw new ArgumentNullException(nameof(name));
            return this;
        }

        /// <summary>
        /// Configures the specified <see cref="SqsWriteConfiguration"/>.
        /// </summary>
        /// <param name="config">The configuration to configure.</param>
        internal override void Configure(SqsWriteConfiguration config)
        {
            base.Configure(config);

            if (QueueName != null)
            {
                config.QueueName = QueueName;
            }

            config.Validate();
        }
    }
}
