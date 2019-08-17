using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization
{
    public class NewtonsoftSerializationFactory : IMessageSerializationFactory
    {
        private readonly NewtonsoftSerializer _serializer = new NewtonsoftSerializer();

        public IMessageSerializer GetSerializer<T>() where T : Message => _serializer;
    }
}
