using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

public interface IMessageBodySerializationFactory
{
    IMessageBodySerializer GetSerializer<T>() where T : Message;
}
