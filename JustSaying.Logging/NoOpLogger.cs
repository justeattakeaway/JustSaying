using System;
using Microsoft.Extensions.Logging;

namespace JustSaying.Logging
{
    public class NoOpLogger : ILogger {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState,Exception,string> formatter)
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }

    public class NoOpLoggerFactory : ILoggerFactory
    {
        private static readonly ILogger Logger = new NoOpLogger();

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return Logger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }
    }
}