namespace JustSaying.Messaging;

/// <summary>
/// Determines the logical type name of an inbound message from a discriminator on the wire (for
/// example the SNS <c>Subject</c>, or a CloudEvents <c>type</c> attribute). Used by multi-type queue
/// subscriptions to select the serializer for each message.
/// </summary>
public interface IMessageTypeDiscriminator
{
    /// <summary>
    /// Attempts to determine the logical type name for an inbound message.
    /// </summary>
    /// <param name="context">The inbound message information.</param>
    /// <param name="typeName">When this method returns, the logical type name, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a type name could be determined; otherwise <see langword="false"/>.</returns>
    bool TryGetMessageTypeName(MessageDiscriminationContext context, out string typeName);
}
