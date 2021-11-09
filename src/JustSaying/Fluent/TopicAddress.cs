using Amazon;

namespace JustSaying.Fluent;

/// <summary>
/// A type that encapsulates an address of an SNS topic.
/// </summary>
internal sealed class TopicAddress
{
    private TopicAddress()
    { }

    /// <summary>
    /// The ARN of the topic.
    /// </summary>
    public string TopicArn { get; private set; }

    /// <summary>
    /// Creates a <see cref="TopicAddress"/> from a topic ARN.
    /// </summary>
    /// <param name="topicArn">The SNS topic ARN.</param>
    /// <returns>A <see cref="TopicAddress"/> created from the ARN.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static TopicAddress FromArn(string topicArn)
    {
        if (!Arn.IsArn(topicArn) || !Arn.TryParse(topicArn, out var arn)) throw new ArgumentException("Must be a valid ARN.", nameof(topicArn));
        if (!string.Equals(arn.Service, "sns", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Must be an ARN for an SNS topic.", nameof(topicArn));
        return new TopicAddress { TopicArn = topicArn };
    }
}