using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Monitoring
{
    public static class LogExtensions
    {
        public static Operation TimedOperation(this ILogger logger, string template, params object[] args)
        {
            return new Operation(logger, template, args);
        }
    }

    public class Operation : IDisposable
    {
        private readonly Stopwatch _stopWatch;
        private readonly ILogger _logger;
        private readonly string _template;
        private readonly object[] _args;

        internal Operation(ILogger logger, string template, object[] args = null)
        {
            _logger = logger;
            _template = template;
            _stopWatch = new Stopwatch();
            _args = args ?? Array.Empty<object>();

            _stopWatch.Start();
        }

        public void Dispose()
        {
            try
            {
                var args = _args.Concat(new object[] {_stopWatch.ElapsedMilliseconds}).ToArray();
                _logger.LogInformation($"{_template} completed in {{Elapsed:0.00}}ms", args);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failure while writing log");
            }
        }
    }
}
