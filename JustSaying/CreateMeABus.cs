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
        public static Func<IAwsClientFactory> DefaultClientFactory = () => new DefaultAwsClientFactory();

        public static JustSayingFluentlyDependencies WithLogging(ILoggerFactory loggerFactory) => 
            new JustSayingFluentlyDependencies { LoggerFactory = loggerFactory};
    }
}
