using System.Net;
using Amazon.Runtime;

namespace JustSaying.AwsTools.MessageHandling;

/// <summary>
/// A class representing the response from publishing a batch of messages.
/// </summary>
public class MessageBatchResponse
{
    /// <summary>
    /// Gets or sets the Ids of the messages that were successfully published.
    /// </summary>
    public IReadOnlyCollection<string> SuccessfulMessageIds { get; set; }

    /// <summary>
    /// Gets or sets the Ids of the messages that failed to publish.
    /// </summary>
    public IReadOnlyCollection<string> FailedMessageIds { get; set; }

    /// <summary>
    /// Gets or sets the response metadata.
    /// </summary>
    public ResponseMetadata ResponseMetadata { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code returned from the publish attempt, if any.
    /// </summary>
    public HttpStatusCode? HttpStatusCode { set; get; }
}
