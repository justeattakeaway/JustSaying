using System.Text.RegularExpressions;

namespace JustSaying.AwsTools.QueueCreation;

/// <summary>
/// Checks SQS queue and SNS topic names against the AWS naming rules, so an invalid name fails when
/// the registration is built instead of as an AWS error on startup.
/// </summary>
internal static class ResourceNameValidator
{
    private const int MaximumQueueNameLength = 80;
    private const int MaximumTopicNameLength = 256;
    private const string ErrorQueueSuffix = "_error";

    private static readonly Regex ValidName = new("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Returns why <paramref name="name"/> is not a valid name for a queue JustSaying creates, or
    /// <see langword="null"/> when it is valid.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <param name="hasErrorQueue">Whether an error queue named <c>{name}_error</c> is created alongside the queue.</param>
    public static string GetQueueNameError(string name, bool hasErrorQueue)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "A queue name cannot be null, empty or only whitespace.";
        }

        if (name.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase))
        {
            return $"The queue name '{name}' is for a FIFO queue. FIFO queues are not supported.";
        }

        if (!ValidName.IsMatch(name))
        {
            return $"The queue name '{name}' is invalid. A queue name can only contain alphanumeric characters, hyphens (-) and underscores (_).";
        }

        if (hasErrorQueue && name.Length + ErrorQueueSuffix.Length > MaximumQueueNameLength)
        {
            // The default naming convention can produce such a name from a long type name, so say how to fix it.
            return $"The queue name '{name}' is too long. Its error queue's name ('{name}{ErrorQueueSuffix}') must be at most {MaximumQueueNameLength} characters, so the queue name can be at most {MaximumQueueNameLength - ErrorQueueSuffix.Length}. " +
                   "Give the queue a shorter name with QueueDestination.Named(...) or WithQueueName(...), or opt out of the error queue with WithNoErrorQueue() on its QueueDestination.";
        }

        if (name.Length > MaximumQueueNameLength)
        {
            return $"The queue name '{name}' is too long. A queue name can be at most {MaximumQueueNameLength} characters.";
        }

        return null;
    }

    /// <summary>
    /// Returns why <paramref name="name"/> is not a valid name for a topic, or <see langword="null"/>
    /// when it is valid.
    /// </summary>
    /// <param name="name">The topic name.</param>
    public static string GetTopicNameError(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "A topic name cannot be null, empty or only whitespace.";
        }

        if (name.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase))
        {
            return $"The topic name '{name}' is for a FIFO topic. FIFO topics are not supported.";
        }

        if (!ValidName.IsMatch(name))
        {
            return $"The topic name '{name}' is invalid. A topic name can only contain alphanumeric characters, hyphens (-) and underscores (_).";
        }

        if (name.Length > MaximumTopicNameLength)
        {
            return $"The topic name '{name}' is too long. A topic name can be at most {MaximumTopicNameLength} characters.";
        }

        return null;
    }
}
