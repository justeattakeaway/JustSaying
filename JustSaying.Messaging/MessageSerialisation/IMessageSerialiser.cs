using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public interface IMessageSerialiser<out T> where T : Message
    {
        T Deserialise(string message);
        string Serialise(Message message);
    }
}