using System;
using JustSaying.AwsTools;
using Microsoft.Extensions.Logging;

namespace JustSaying
{
    /// <summary>
    /// A class representing a builder for JustSaying buses.
    /// </summary>
    public class BusBuilder
    {
        private IAwsClientFactory ClientFactory { get; set; } = new DefaultAwsClientFactory();

        private ILoggerFactory LoggerFactory { get; set; }

        /// <summary>
        /// Specifies the <see cref="IAwsClientFactory"/> to use.
        /// </summary>
        /// <param name="clientFactory">The <see cref="IAwsClientFactory"/> to use.</param>
        /// <returns>
        /// The current <see cref="BusBuilder"/>.
        /// </returns>
        public BusBuilder WithClientFactory(IAwsClientFactory clientFactory)
        {
            ClientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            return this;
        }

        /// <summary>
        /// Specifies the <see cref="ILoggerFactory"/> to use.
        /// </summary>
        /// <param name="clientFactory">The <see cref="ILoggerFactory"/> to use.</param>
        /// <returns>
        /// The current <see cref="BusBuilder"/>.
        /// </returns>
        public BusBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            return this;
        }

        /// <summary>
        /// Create a new bus.
        /// </summary>
        /// <returns>
        /// The <see cref="JustSayingFluentlyDependencies"/> to use to create the bus.
        /// </returns>
        public JustSayingFluentlyDependencies CreateMeABus()
            => new JustSayingFluentlyDependencies() { LoggerFactory = LoggerFactory }; // TODO ClientFactory not used here yet - lots of rework to make this the root
    }
}
