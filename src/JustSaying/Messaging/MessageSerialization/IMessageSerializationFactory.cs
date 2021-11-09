using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization;

public interface IMessageSerializationFactory
{
    IMessageSerializer GetSerializer<T>() where T : Message;
}