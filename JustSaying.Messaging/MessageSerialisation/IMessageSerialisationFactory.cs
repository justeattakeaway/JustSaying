using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public interface IMessageSerialisationFactory
    {
        IMessageSerialiser<Message> GetSerialiser<T>() where T : Message;
    }
}