using System;
using JustSaying.AwsTools.QueueCreation;

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
        private Func<Exception, object, bool> Handler { get; set; }

        /// <summary>
        /// Configures an error handler to use.
        /// </summary>
        /// <param name="handler">A delegate to a method to call when an error occurs.</param>
        /// <returns>
        /// The current <see cref="SnsWriteConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        public SnsWriteConfigurationBuilder WithErrorHandler<T>(Func<Exception, T, bool> handler) where T : class
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }
            var wrappedHandler = new Func<Exception, object, bool>((ex, message) => handler(ex, (T)message));
            Handler = wrappedHandler;
            return this;
        }

        /// <summary>
        /// Configures the specified <see cref="SnsWriteConfiguration"/>.
        /// </summary>
        /// <param name="config">The configuration to configure.</param>
        internal void Configure(SnsWriteConfiguration config)
        {
            if (Handler != null)
            {
                config.HandleException = Handler;
            }
        }
    }
}
