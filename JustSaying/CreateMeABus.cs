using System;
using JustSaying.AwsTools;
using Microsoft.Extensions.Logging;

namespace JustSaying
{
    public static class CreateMeABus
    {
        /// <summary>
        /// Allows to override default <see cref="IAwsClientFactory"/> globally.
        /// </summary>
        ////[Obsolete("Use the BusBuilder class to create message buses.")]
        public static Func<IAwsClientFactory> DefaultClientFactory { get; set; }
            = () => new DefaultAwsClientFactory();

        ////[Obsolete("Use the BusBuilder class to create message buses.")]
        public static JustSayingFluentlyDependencies WithLogging(ILoggerFactory loggerFactory) =>
            new JustSayingFluentlyDependencies { LoggerFactory = loggerFactory};
    }
}
