namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Adapts a strongly-typed <see cref="IMessageBodySerializer{TMessage}"/> to the type-erased
/// <see cref="IMessageBodySerializer"/> used at the internal dispatch boundary. The cast from
/// <see cref="object"/> to <typeparamref name="TMessage"/> is free for reference types, and the
/// inner serializer performs the actual (generic, trim/AOT-friendly) serialization.
/// </summary>
/// <typeparam name="TMessage">The concrete message type the inner serializer handles.</typeparam>
internal class ErasedMessageBodySerializer<TMessage>(IMessageBodySerializer<TMessage> inner) : IMessageBodySerializer
    where TMessage : class
{
    public string Serialize(object message) => inner.Serialize((TMessage)message);

    public object Deserialize(string message) => inner.Deserialize(message);
}

/// <summary>
/// An <see cref="ErasedMessageBodySerializer{TMessage}"/> over a self-describing serializer, which keeps
/// the <see cref="ISelfDescribingMessageBodySerializer"/> marker visible across the type-erased boundary.
/// </summary>
/// <typeparam name="TMessage">The concrete message type the inner serializer handles.</typeparam>
internal sealed class SelfDescribingErasedMessageBodySerializer<TMessage>(IMessageBodySerializer<TMessage> inner)
    : ErasedMessageBodySerializer<TMessage>(inner), ISelfDescribingMessageBodySerializer
    where TMessage : class;
