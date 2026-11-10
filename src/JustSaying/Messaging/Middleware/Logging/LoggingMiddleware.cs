using System.Diagnostics;
using JustSaying.Extensions;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Middleware.Logging;

/// <summary>
/// A middleware that logs a rich information or warning event when a message is handled.
/// </summary>
/// <remarks>
/// Constructs a <see cref="LoggingMiddleware"/>.
/// </remarks>
/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to write logs to.</param>
public sealed class LoggingMiddleware(ILoggerFactory loggerFactory) : MiddlewareBase<HandleMessageContext, bool>
{
    private readonly ILogger<LoggingMiddleware> _logger =
        loggerFactory?.CreateLogger<LoggingMiddleware>()
        ?? throw new ArgumentNullException(nameof(loggerFactory));

    /// <summary>
    /// Gets or sets the provider used to read the logged message identity. A subscription's pipeline
    /// sets this to the bus's provider when it is built.
    /// </summary>
    internal IMessageMetadataProvider MetadataProvider { get; set; } = DefaultMessageMetadataProvider.Instance;

    protected override async Task<bool> RunInnerAsync(HandleMessageContext context, Func<CancellationToken, Task<bool>> func, CancellationToken stoppingToken)
    {
        using var disposable = _logger.BeginScope(new Dictionary<string, object>()
        {
            ["MessageSource"] = context.QueueName,
            ["SourceType"] = "Queue"
        });

        var watch = Stopwatch.StartNew();
        bool dispatchSuccessful = false;
        try
        {
            dispatchSuccessful = await func(stoppingToken).ConfigureAwait(false);
            watch.Stop();
            return dispatchSuccessful;
        }
        finally
        {
            const string MessageTemplate = "{Status} handling message with Id '{MessageId}' of type {MessageType} in {TimeToHandle}ms.";

            if (dispatchSuccessful)
            {
                _logger.LogInformation(
                    context.HandledException,
                    MessageTemplate,
                    "Succeeded",
                    MessageIdentity.GetId(context.Message, MetadataProvider),
                    context.MessageType.ToReadableFullName(),
                    watch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    context.HandledException,
                    MessageTemplate,
                    "Failed",
                    MessageIdentity.GetId(context.Message, MetadataProvider),
                    context.MessageType.ToReadableFullName(),
                    watch.ElapsedMilliseconds);
            }
        }
    }
}
