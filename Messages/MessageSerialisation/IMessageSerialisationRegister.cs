using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Messaging.MessageSerialisation
{
    public interface IMessageSerialisationRegister
    {
        IMessageSerialiser<Message> GetSerialiser(string objectType);
    }
}