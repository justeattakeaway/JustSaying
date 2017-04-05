using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public interface IMessageSerialisationFactory
    {
        IMessageSerialiser GetSerialiser<T>() where T : Message;
    }
}