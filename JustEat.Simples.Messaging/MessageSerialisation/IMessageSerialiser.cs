using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.Messaging.MessageSerialisation
{
    public interface IMessageSerialiser<out T> where T : Message
    {
        string Key { get; }
        T Deserialise(string message);
        string Serialise(Message message);
    }
}