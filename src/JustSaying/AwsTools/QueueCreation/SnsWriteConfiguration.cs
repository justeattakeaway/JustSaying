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
    /// Gets or sets a function for custom error handling on a per-notification basis.
    /// </summary>
    /// <remarks>
    /// This is an extension point enabling custom error handling, including the ability to handle raised exceptions.
    /// </remarks>
    /// <returns>A function that takes an Exception and a Message as parameters and returns a boolean indicating whether the exception has been handled.</returns>
    public Func<Exception, Message, bool> HandleException { get; set; }
}
