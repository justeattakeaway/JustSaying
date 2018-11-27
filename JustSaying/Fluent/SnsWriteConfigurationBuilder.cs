using System;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Models;

namespace JustSaying.Fluent
{
    /// <summary>
    /// A class representing a builder for configuring instances of <see cref="SnsWriteConfiguration"/>. This class cannot be inherited.
    /// </summary>
    public sealed class SnsWriteConfigurationBuilder
    {
        /// <summary>
        /// Gets or sets the error callback to use.
        /// </summary>
        private Func<Exception, Message, bool> OnError { get; set; }

        /// <summary>
        /// Configures an error handler to use.
        /// </summary>
        /// <param name="action">A delegate to a method to call when an error occurs.</param>
        /// <returns>
        /// The current <see cref="SnsWriteConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        public SnsWriteConfigurationBuilder WithErrorHandler(Func<Exception, Message, bool> action)
        {
            OnError = action ?? throw new ArgumentNullException(nameof(action));
            return this;
        }

        /// <summary>
        /// Configures the specified <see cref="SqsWriteConfiguration"/>.
        /// </summary>
        /// <param name="config">The configuration to configure.</param>
        internal void Configure(SnsWriteConfiguration config)
        {
            if (OnError != null)
            {
                config.HandleException = OnError;
            }
        }
    }
}
