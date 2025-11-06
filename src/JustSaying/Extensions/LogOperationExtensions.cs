using System;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Extensions
{
    internal static class LogOperationExtensions
    {
        internal static IDisposable Time(this ILogger logger, LogLevel level, string logMessage, params object[] args)
        {
            return new LogOperation(logger, level, logMessage, args);
        }

        internal static IDisposable Time(this ILogger logger, string logMessage, params object[] args)
        {
            return new LogOperation(logger, LogLevel.Information, logMessage, args);
        }
    }
}
