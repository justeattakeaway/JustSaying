using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Messaging.MessageSerialisation
{
    public interface IMessageSerialiser<out T> where T : Message
    {
        string Key { get; }
        T Deserialise(string message);
        string Serialise(Message message);
    }
}