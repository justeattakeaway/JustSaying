using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public class NewtonsoftSerialisationFactory : IMessageSerialisationFactory
    {
        public IMessageSerialiser GetSerialiser<T>() where T : Message
        {
            return new NewtonsoftSerialiser();
        }
    }
}