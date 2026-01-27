namespace JustSaying.Extensions.Kafka.Configuration;

/// <summary>
/// Defines how message retries are handled.
/// </summary>
public enum RetryMode
{
    /// <summary>
    /// Retries happen in-process with partition pause.
    /// Lower cost (single DLT topic), but partition is blocked during retry backoff.
    /// This is the default mode.
    /// </summary>
    InProcess,

    /// <summary>
    /// Retries happen via separate topics (topic chaining pattern).
    /// Higher cost (multiple topics), but non-blocking.
    /// Requires configuring separate consumers for retry topics.
    /// </summary>
    TopicChaining
}

