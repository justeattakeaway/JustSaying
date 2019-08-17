using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization
{
    public class SystemTextJsonSerializationFactory : IMessageSerializationFactory
    {
        private readonly SystemTextJsonSerializer _serializer = new SystemTextJsonSerializer();

        public IMessageSerializer GetSerializer<T>() where T : Message => _serializer;
    }
}
