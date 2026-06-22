namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// A bidirectional registry that maps a message's CLR <see cref="Type"/> to its logical wire name
/// (the SNS <c>Subject</c> today, and the CloudEvents <c>type</c> attribute in future) and back.
/// <para>
/// This generalises <see cref="IMessageSubjectProvider"/>: the outbound direction
/// (<see cref="GetLogicalName"/>) preserves the existing subject behaviour, while the reverse
/// direction (<see cref="TryResolveType"/>) lets an inbound message be resolved to a CLR type by its
/// logical name — the building block for type-based routing (e.g. consuming CloudEvents produced by
/// other platforms on a queue that carries more than one event type).
/// </para>
/// </summary>
public interface IMessageTypeRegistry
{
    /// <summary>
    /// Gets the logical name for the specified message type, recording the mapping so that the type
    /// can later be resolved from that name via <see cref="TryResolveType"/>.
    /// </summary>
    /// <param name="messageType">The message CLR type.</param>
    /// <returns>The logical name for the type.</returns>
    string GetLogicalName(Type messageType);

    /// <summary>
    /// Associates a CLR type with an explicit logical name, overriding any name that would otherwise
    /// be derived for it.
    /// </summary>
    /// <param name="messageType">The message CLR type.</param>
    /// <param name="logicalName">The logical name to associate with <paramref name="messageType"/>.</param>
    void Register(Type messageType, string logicalName);

    /// <summary>
    /// Attempts to resolve a CLR type from a previously-registered logical name.
    /// </summary>
    /// <param name="logicalName">The logical name to resolve.</param>
    /// <param name="messageType">When this method returns, the resolved type, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a type was registered for the name; otherwise <see langword="false"/>.</returns>
    bool TryResolveType(string logicalName, out Type messageType);
}
