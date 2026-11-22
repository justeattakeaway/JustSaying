using JustSaying.AwsTools.MessageHandling;
using JustSaying.Models;

namespace JustSaying.AwsTools.QueueCreation;

/// <summary>
/// Represents the configuration for writing messages to Amazon SNS (Simple Notification Service).
/// </summary>
public class SnsWriteConfiguration
{
    private string _subject;

    /// <summary>
    /// Gets or sets the server-side encryption settings for the SNS topic.
    /// </summary>
    public ServerSideEncryption Encryption { get; set; }

    /// <summary>
    /// Gets or sets the compression options for publishing messages.
    /// </summary>
    public PublishCompressionOptions CompressionOptions { get; set; }

    /// <summary>
    /// Gets or sets the maximum size, in bytes, of a message the topic will accept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set, JustSaying applies this as the <c>MaximumMessageSize</c> attribute on the topic it creates,
    /// and uses it as the budget for compression and batching. Valid values are between
    /// <see cref="JustSayingConstants.MinimumSnsMessageSize"/> and <see cref="JustSayingConstants.MaximumSnsMessageSize"/>.
    /// </para>
    /// <para>
    /// When <see langword="null"/> (the default), the topic keeps the SNS default of
    /// <see cref="JustSayingConstants.DefaultSnsMaximumMessageSize"/> and JustSaying does not touch the attribute.
    /// </para>
    /// <para>
    /// A topic with this set above 256 KiB supports only Amazon SQS, Amazon Data Firehose and AWS Lambda
    /// subscriptions, and at most 100 subscriptions in total.
    /// See https://docs.aws.amazon.com/sns/latest/dg/large-message-payloads.html.
    /// </para>
    /// </remarks>
    public int? MaximumMessageSize { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the message should be treated as a raw message.
    /// </summary>
    public bool IsRawMessage { get; set; }

    /// <summary>
    /// Gets or sets the subject of the SNS message.
    /// </summary>
    public string Subject
    {
        get => _subject;
        set
        {
            _subject = value;
            SubjectSet = true;
        }
    }

    /// <summary>
    /// Internal property to track whether the Subject has been set.
    /// </summary>
    internal bool SubjectSet { get; private set; }

    /// <summary>
    /// Gets or sets a delegate for custom error handling on a per-notification basis.
    /// </summary>
    /// <remarks>
    /// This is an extension point enabling custom error handling, including the ability to handle raised exceptions.
    /// </remarks>
    /// <returns>A function that takes an Exception and a Message as parameters and returns a boolean indicating whether the exception has been handled.</returns>
    public Func<Exception, Message, bool> HandleException { get; set; }

    /// <summary>
    /// Gets the maximum message size the topic accepts, falling back to the SNS default where none is configured.
    /// </summary>
    internal int EffectiveMaximumMessageSize
        => MaximumMessageSize ?? JustSayingConstants.DefaultSnsMaximumMessageSize;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <exception cref="ConfigurationErrorsException">The configuration is not valid.</exception>
    public void Validate()
    {
        if (MaximumMessageSize is { } maximumMessageSize &&
            (maximumMessageSize < JustSayingConstants.MinimumSnsMessageSize ||
             maximumMessageSize > JustSayingConstants.MaximumSnsMessageSize))
        {
            throw new ConfigurationErrorsException(
                $"Invalid configuration. {nameof(MaximumMessageSize)} must be between {JustSayingConstants.MinimumSnsMessageSize} and {JustSayingConstants.MaximumSnsMessageSize} bytes.");
        }
    }
}
