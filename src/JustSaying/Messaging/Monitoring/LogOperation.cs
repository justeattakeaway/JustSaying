using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Monitoring
{
    internal sealed class LogOperation : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _message;
        private readonly object[] _args;
        private readonly Stopwatch _watch;
        private readonly LogLevel _logLevel;

        public LogOperation(ILogger logger, LogLevel logLevel, string message, params object[] args)
        {
            _logger = logger;
            _logLevel = logLevel;
            _message = message;
            _args = args;
            _watch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _watch.Stop();

            var args = _args.Concat(new object[] { _watch.Elapsed }).ToArray();

            switch (_logLevel)
            {
                case LogLevel.Trace:
                    _logger.LogTrace(_message, args);
                    return;
                case LogLevel.Debug:
                    _logger.LogDebug(_message, args);
                    return;
                case LogLevel.Information:
                    _logger.LogInformation(_message, args);
                    return;
                case LogLevel.Warning:
                    _logger.LogWarning(_message, args);
                    return;
                case LogLevel.Error:
                    _logger.LogError(_message, args);
                    return;
                case LogLevel.Critical:
                    _logger.LogCritical(_message, args);
                    return;
                case LogLevel.None:
                    return;
            }
        }
    }
}
