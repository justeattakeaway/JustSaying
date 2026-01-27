namespace JustSaying.Extensions.Kafka.Configuration;

/// <summary>
/// Configuration for message retry behavior.
/// </summary>
public class RetryConfiguration
{
    /// <summary>
    /// The retry mode. Default is InProcess (cost-optimized).
    /// </summary>
    public RetryMode Mode { get; set; } = RetryMode.InProcess;

    /// <summary>
    /// Maximum number of retry attempts before sending to DLT.
    /// Only used when Mode = InProcess.
    /// Default is 3.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Initial backoff delay between retries.
    /// Only used when Mode = InProcess.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Whether to use exponential backoff.
    /// Only used when Mode = InProcess.
    /// Default is true.
    /// </summary>
    public bool ExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Maximum backoff delay when using exponential backoff.
    /// Only used when Mode = InProcess.
    /// Default is 60 seconds.
    /// </summary>
    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Validates the retry configuration.
    /// </summary>
    public void Validate()
    {
        if (MaxRetryAttempts < 0)
        {
            throw new InvalidOperationException("MaxRetryAttempts must be non-negative.");
        }

        if (InitialBackoff < TimeSpan.Zero)
        {
            throw new InvalidOperationException("InitialBackoff must be non-negative.");
        }

        if (MaxBackoff < InitialBackoff)
        {
            throw new InvalidOperationException("MaxBackoff must be greater than or equal to InitialBackoff.");
        }
    }
}

