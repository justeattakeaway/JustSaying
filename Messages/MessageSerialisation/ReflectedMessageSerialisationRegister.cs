using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Messaging.MessageSerialisation
{
    public class ReflectedMessageSerialisationRegister : IMessageSerialisationRegister
    {
        public ReflectedMessageSerialisationRegister()
        {
            // REFLECT TO GET!
        }

        public IMessageSerialiser<Message> GetSerialiser(string objectType)
        {
            throw new System.NotImplementedException();
        }
    }
}