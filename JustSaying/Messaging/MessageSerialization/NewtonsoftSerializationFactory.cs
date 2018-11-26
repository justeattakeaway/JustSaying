using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization
{
    public class NewtonsoftSerializationFactory : IMessageSerializationFactory
    {
        public IMessageSerializer GetSerializer<T>() where T : Message => new NewtonsoftSerializer();
    }
}
