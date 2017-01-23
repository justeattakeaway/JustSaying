using System;
using Microsoft.Extensions.Logging;

namespace JustSaying.Logging
{
    public static class LoggerExtensions
    {
        public static void Warn(this ILogger logger, string message)
        {
            logger.LogWarning(message);
        }

        public static void Warn(this ILogger logger, Exception exception, string message)
        {
            logger.LogWarning(0, exception, message);
        }

        public static void Info(this ILogger logger, string message)
        {
            logger.LogInformation(message);
        }

        public static void Trace(this ILogger logger, string message)
        {
            logger.LogTrace(message);
        }

        public static void Error(this ILogger logger, string message)
        {
            logger.LogError(message);
        }

        public static void Error(this ILogger logger, Exception exception, string message)
        {
            logger.LogError(0, exception, message);
        }
    }
}
