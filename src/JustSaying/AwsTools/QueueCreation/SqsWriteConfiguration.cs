using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.AwsTools.QueueCreation;

public class SqsWriteConfiguration : SqsBasicConfiguration
{
    public PublishCompressionOptions CompressionOptions { get; set; }

    /// <summary>
    /// Gets or sets the maximum size, in bytes, of a message the queue will accept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SQS defaults a queue to <see cref="JustSayingConstants.DefaultSqsMaximumMessageSize"/>, so this only needs
    /// setting for a queue whose <c>MaximumMessageSize</c> attribute has been set lower, which is common for
    /// queues created by infrastructure tooling that still defaults to 256 KiB. The value is used as the budget
    /// for compression. It does not affect batching, SQS allows a batch to add up to 1 MiB whatever the queue's
    /// limit. Valid values are between
    /// <see cref="JustSayingConstants.MinimumSqsMessageSize"/> and <see cref="JustSayingConstants.MaximumSqsMessageSize"/>.
    /// </para>
    /// <para>
    /// This describes the queue rather than configuring it, JustSaying does not apply it as a queue attribute.
    /// </para>
    /// </remarks>
    public int? MaximumMessageSize { get; set; }

    /// <summary>
    /// Gets the maximum message size the queue accepts, falling back to the SQS default where none is configured.
    /// </summary>
    internal int EffectiveMaximumMessageSize
        => MaximumMessageSize ?? JustSayingConstants.DefaultSqsMaximumMessageSize;

    /// <inheritdoc />
    protected override void OnValidating()
        => ValidateMaximumMessageSize();

    /// <summary>
    /// Validates <see cref="MaximumMessageSize"/>.
    /// </summary>
    /// <exception cref="ConfigurationErrorsException">The maximum message size is not valid.</exception>
    internal void ValidateMaximumMessageSize()
    {
        if (MaximumMessageSize is { } maximumMessageSize &&
            (maximumMessageSize < JustSayingConstants.MinimumSqsMessageSize ||
             maximumMessageSize > JustSayingConstants.MaximumSqsMessageSize))
        {
            throw new ConfigurationErrorsException(
                $"Invalid configuration. {nameof(MaximumMessageSize)} must be between {JustSayingConstants.MinimumSqsMessageSize} and {JustSayingConstants.MaximumSqsMessageSize} bytes.");
        }
    }
}
