namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Adapts a strongly-typed <see cref="IMessageBodySerializer{TMessage}"/> to the type-erased
/// <see cref="IMessageBodySerializer"/> used at the internal dispatch boundary. The cast from
/// <see cref="object"/> to <typeparamref name="TMessage"/> is free for reference types, and the
/// inner serializer performs the actual (generic, trim/AOT-friendly) serialization.
/// </summary>
/// <typeparam name="TMessage">The concrete message type the inner serializer handles.</typeparam>
internal sealed class ErasedMessageBodySerializer<TMessage>(IMessageBodySerializer<TMessage> inner) : IMessageBodySerializer, IContextProvidingMessageBodySerializer
    where TMessage : class
{
    public string Serialize(object message) => inner.Serialize((TMessage)message);

    public object Deserialize(string message) => inner.Deserialize(message);

    public object Deserialize(string message, out MessageHandling.MessageContextFactory contextFactory)
    {
        if (inner is IContextProvidingMessageBodySerializer<TMessage> contextProviding)
        {
            return contextProviding.Deserialize(message, out contextFactory);
        }

        contextFactory = null;
        return inner.Deserialize(message);
    }
}
