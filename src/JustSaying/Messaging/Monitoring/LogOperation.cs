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

        public LogOperation(ILogger logger, string message, params object[] args)
        {
            _logger = logger;
            _message = message;
            _args = args;
            _watch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _watch.Stop();

            var args = _args.Concat(new object[] { _watch.Elapsed }).ToArray();

            _logger.LogInformation($"{_message} completed in {{Duration}}ms", args);
        }
    }
}
