using JustSaying.Messaging.Middleware;

namespace JustSaying.Extensions.HeaderPropagation;

/// <summary>
/// Subscribe-side middleware that filters messages based on a named message attribute value.
/// Use this for feature-branch routing: only process messages where the attribute matches the expected value.
/// </summary>
public sealed class HeaderValueFilterMiddleware : MiddlewareBase<HandleMessageContext, bool>
{
    private readonly string _headerName;
    private readonly string? _expectedValue;

    /// <summary>
    /// Initializes a new instance of <see cref="HeaderValueFilterMiddleware"/>.
    /// </summary>
    /// <param name="headerName">The message attribute name to match against.</param>
    /// <param name="expectedValue">
    /// The expected attribute value. When <see langword="null"/>, the message is processed only if
    /// the attribute is absent (default/production traffic). When non-null, the message is processed
    /// only if the attribute equals this value (ordinal, case-sensitive).
    /// </param>
    public HeaderValueFilterMiddleware(string headerName, string? expectedValue = null)
    {
        _headerName = headerName ?? throw new ArgumentNullException(nameof(headerName));
        _expectedValue = expectedValue;
    }

    /// <inheritdoc/>
    protected override async Task<bool> RunInnerAsync(
        HandleMessageContext context,
        Func<CancellationToken, Task<bool>> func,
        CancellationToken stoppingToken)
    {
        var actual = context.MessageAttributes.Get(_headerName)?.StringValue;

        if (!string.Equals(actual, _expectedValue, StringComparison.Ordinal))
            return true; // acknowledge without processing

        return await func(stoppingToken).ConfigureAwait(false);
    }
}
