using System.Collections.Concurrent;

namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// The default <see cref="IMessageTypeRegistry"/>. Derives logical names from an
/// <see cref="IMessageSubjectProvider"/> (preserving the existing subject behaviour) and records each
/// mapping so it can be resolved in reverse.
/// </summary>
/// <remarks>
/// Reverse resolution is only as unique as the underlying naming scheme. The default
/// <see cref="NonGenericMessageSubjectProvider"/> uses the unqualified type name, so two message types
/// that share a name (in different namespaces) collide in the reverse map; a namespaced scheme (such
/// as the reverse-DNS CloudEvents <c>type</c>) avoids this.
/// </remarks>
internal sealed class MessageTypeRegistry(IMessageSubjectProvider subjectProvider) : IMessageTypeRegistry
{
    private readonly IMessageSubjectProvider _subjectProvider = subjectProvider ?? throw new ArgumentNullException(nameof(subjectProvider));
    private readonly ConcurrentDictionary<Type, string> _namesByType = new();
    private readonly ConcurrentDictionary<string, Type> _typesByName = new(StringComparer.Ordinal);

    public string GetLogicalName(Type messageType)
    {
        if (messageType is null) throw new ArgumentNullException(nameof(messageType));

        return _namesByType.GetOrAdd(messageType, type =>
        {
            string name = _subjectProvider.GetSubjectForType(type);
            _typesByName[name] = type;
            return name;
        });
    }

    public void Register(Type messageType, string logicalName)
    {
        if (messageType is null) throw new ArgumentNullException(nameof(messageType));
        if (string.IsNullOrEmpty(logicalName)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(logicalName));

        _namesByType[messageType] = logicalName;
        _typesByName[logicalName] = messageType;
    }

    public bool TryResolveType(string logicalName, out Type messageType)
    {
        if (string.IsNullOrEmpty(logicalName))
        {
            messageType = null;
            return false;
        }

        return _typesByName.TryGetValue(logicalName, out messageType);
    }
}
