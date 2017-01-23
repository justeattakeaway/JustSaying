using System;
using JustSaying.AwsTools;
using JustSaying.Logging;
using Microsoft.Extensions.Logging;

namespace JustSaying
{
    public static class CreateMeABus
    {
        /// <summary>
        /// Allows to override default <see cref="IAwsClientFactory"/> globally.
        /// </summary>
        public static Func<IAwsClientFactory> DefaultClientFactory = () => new DefaultAwsClientFactory();

        public static JustSayingFleuntlyLogging WithLogging(ILoggerFactory loggerFactory) => 
            new JustSayingFleuntlyLogging {LoggerFactory = loggerFactory};

        public static JustSayingFleuntlyLogging WithNoLogging() => WithLogging(new NoOpLoggerFactory());
    }
}