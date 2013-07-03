using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Messaging.MessageSerialisation
{
    public interface IMessageSerialiser<out T> where T : Message
    {
        string Key { get; }
        T Deserialised(string message);
        string Serialised(Message message);
    }
}