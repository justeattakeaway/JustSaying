namespace JustSaying.Messaging;

/// <summary>
/// Determines the logical type name of an inbound message from a discriminator on the wire (for
/// example the SNS <c>Subject</c>, or a CloudEvents <c>type</c> attribute). Used by multi-type queue
/// subscriptions to select the serializer for each message.
/// </summary>
/// <remarks>
/// When no discriminator in the chain resolves a registered type, the error logged for the message names
/// each discriminator by its <see cref="object.ToString"/> along with the value it found, so override
/// <see cref="object.ToString"/> to give a custom discriminator a readable name (for example <c>subject</c>).
/// </remarks>
public interface IMessageTypeDiscriminator
{
    /// <summary>
    /// Attempts to determine the logical type name for an inbound message.
    /// </summary>
    /// <param name="context">The inbound message information.</param>
    /// <param name="typeName">When this method returns, the logical type name, or <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if a type name could be determined; otherwise <see langword="false"/>.
    /// Return <see langword="false"/> for a message this discriminator doesn't recognise, so the next
    /// discriminator in the chain can try: the first to return a name decides the message's type.
    /// </returns>
    bool TryGetMessageTypeName(MessageDiscriminationContext context, out string typeName);
}
