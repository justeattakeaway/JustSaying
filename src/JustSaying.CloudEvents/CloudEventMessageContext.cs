using JustSaying.Messaging.MessageHandling;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.CloudEvents;

/// <summary>
/// A <see cref="MessageContext"/> for a message that arrived as a structured-mode
/// <see href="https://github.com/cloudevents/spec">CloudEvents 1.0</see> envelope, exposing the
/// envelope's context attributes. Handlers can observe it through
/// <see cref="IMessageContextReader"/>, either by downcasting <see cref="IMessageContextReader.MessageContext"/>
/// or via <see cref="MessageContextReaderExtensions.GetCloudEventContext"/>.
/// </summary>
public sealed class CloudEventMessageContext : MessageContext
{
    /// <summary>
    /// Creates an instance of <see cref="CloudEventMessageContext"/>.
    /// </summary>
    /// <param name="message">The <see cref="Amazon.SQS.Model.Message"/> currently being processed.</param>
    /// <param name="queueUri">The URI of the SQS queue the message is from.</param>
    /// <param name="messageAttributes">The <see cref="MessageAttributes"/> from the message.</param>
    /// <param name="specVersion">The CloudEvents <c>specversion</c> attribute.</param>
    /// <param name="id">The CloudEvents <c>id</c> attribute.</param>
    /// <param name="source">The CloudEvents <c>source</c> attribute.</param>
    /// <param name="type">The CloudEvents <c>type</c> attribute.</param>
    /// <param name="time">The CloudEvents <c>time</c> attribute, if present.</param>
    /// <param name="dataContentType">The CloudEvents <c>datacontenttype</c> attribute, if present.</param>
    /// <param name="dataSchema">The CloudEvents <c>dataschema</c> attribute, if present.</param>
    /// <param name="subject">The CloudEvents <c>subject</c> attribute, if present.</param>
    /// <param name="extensions">The envelope's extension attributes, keyed by attribute name.</param>
    public CloudEventMessageContext(
        SQSMessage message,
        Uri queueUri,
        MessageAttributes messageAttributes,
        string specVersion,
        string id,
        Uri source,
        string type,
        DateTimeOffset? time = null,
        string dataContentType = null,
        Uri dataSchema = null,
        string subject = null,
        IReadOnlyDictionary<string, string> extensions = null)
        : base(message, queueUri, messageAttributes)
    {
        SpecVersion = specVersion ?? throw new ArgumentNullException(nameof(specVersion));
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Time = time;
        DataContentType = dataContentType;
        DataSchema = dataSchema;
        Subject = subject;
        Extensions = extensions ?? EmptyExtensions;
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyExtensions = new Dictionary<string, string>(0);

    /// <summary>
    /// Gets the CloudEvents <c>specversion</c> attribute.
    /// </summary>
    public string SpecVersion { get; }

    /// <summary>
    /// Gets the CloudEvents <c>id</c> attribute.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the CloudEvents <c>source</c> attribute.
    /// </summary>
    public Uri Source { get; }

    /// <summary>
    /// Gets the CloudEvents <c>type</c> attribute.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the CloudEvents <c>time</c> attribute, if present.
    /// </summary>
    public DateTimeOffset? Time { get; }

    /// <summary>
    /// Gets the CloudEvents <c>datacontenttype</c> attribute, if present.
    /// </summary>
    public string DataContentType { get; }

    /// <summary>
    /// Gets the CloudEvents <c>dataschema</c> attribute, if present.
    /// </summary>
    public Uri DataSchema { get; }

    /// <summary>
    /// Gets the CloudEvents <c>subject</c> attribute, if present.
    /// </summary>
    public string Subject { get; }

    /// <summary>
    /// Gets the envelope's extension attributes (top-level members other than the CloudEvents core
    /// attributes and <c>data</c>), keyed by attribute name. Values are the attributes' string
    /// representations.
    /// </summary>
    public IReadOnlyDictionary<string, string> Extensions { get; }
}
