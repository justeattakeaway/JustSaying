using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public class NewtonsoftSerialisationFactory : IMessageSerialisationFactory
    {
        public IMessageSerialiser<Message> GetSerialiser<T>() where T : Message
        {
            return new NewtonsoftSerialiser<T>();
        }
    }
}