using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Monitoring;

internal sealed class LogOperation(ILogger logger, LogLevel logLevel, string message, params object[] args) : IDisposable
{
    private readonly string _message = message;
    private readonly object[] _args = args;
    private readonly Stopwatch _watch = Stopwatch.StartNew();

    public void Dispose()
    {
        _watch.Stop();

        var message = $"{_message} finished in {0:0}ms";
        var args = _args.Concat([_watch.Elapsed.TotalMilliseconds]).ToArray();

#pragma warning disable CA2254
        switch (logLevel)
        {
            case LogLevel.Trace:
                logger.LogTrace(message, args);
                return;
            case LogLevel.Debug:
                logger.LogDebug(message, args);
                return;
            case LogLevel.Information:
                logger.LogInformation(message, args);
                return;
            case LogLevel.Warning:
                logger.LogWarning(message, args);
                return;
            case LogLevel.Error:
                logger.LogError(message, args);
                return;
            case LogLevel.Critical:
                logger.LogCritical(message, args);
                return;
            case LogLevel.None:
                return;
            default:
                break;
        }
#pragma warning restore CA2254
    }
}
