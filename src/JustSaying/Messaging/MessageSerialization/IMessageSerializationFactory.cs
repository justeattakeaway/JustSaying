using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

public interface IMessageSerializationFactory
{
    IMessageSerializer GetSerializer<TMessage>() where TMessage : class;
}
