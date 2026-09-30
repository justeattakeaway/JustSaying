namespace JustSaying.Messaging;

/// <summary>
/// An <see cref="IMessageTypeDiscriminator"/> that uses the SNS <c>Subject</c> as the logical type
/// name. This is JustSaying's native discriminator; the <c>Subject</c> a publisher emits is the
/// message type's logical name (by default its unqualified type name).
/// </summary>
public sealed class SubjectMessageTypeDiscriminator : IMessageTypeDiscriminator
{
    /// <inheritdoc />
    public bool TryGetMessageTypeName(MessageDiscriminationContext context, out string typeName)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        typeName = context.Subject;
        return !string.IsNullOrEmpty(typeName);
    }
}
