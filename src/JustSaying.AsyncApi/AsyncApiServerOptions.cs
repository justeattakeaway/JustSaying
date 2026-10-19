namespace JustSaying.AsyncApi;

/// <summary>
/// A server the application's channels are available on, written to the AsyncAPI document's
/// <c>servers</c>. See <see cref="AsyncApiOptions.Servers"/> for how channels are bound to servers.
/// </summary>
public sealed class AsyncApiServerOptions
{
    /// <summary>
    /// Gets or sets the host name (and optionally port) of the server, for example
    /// <c>sns.eu-west-1.amazonaws.com</c> or <c>localhost:4566</c>. Required.
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// Gets or sets the protocol the server supports: <c>sns</c> for Amazon SNS topics, or
    /// <c>sqs</c> for Amazon SQS queues. Required.
    /// </summary>
    public string Protocol { get; set; }

    /// <summary>
    /// Gets or sets a description of the server, for example the environment it belongs to.
    /// </summary>
    public string Description { get; set; }
}
