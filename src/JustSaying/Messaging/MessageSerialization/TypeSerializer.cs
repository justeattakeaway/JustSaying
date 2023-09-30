namespace JustSaying.Messaging.MessageSerialization;

public class TypeSerializer(Type type, IMessageSerializer serializer)
{
    public Type Type { get; private set; } = type;
    public IMessageSerializer Serializer { get; private set; } = serializer;
}
