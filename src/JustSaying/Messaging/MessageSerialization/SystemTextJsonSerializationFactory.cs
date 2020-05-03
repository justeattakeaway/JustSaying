using System.Text.Json;

namespace JustSaying.Messaging.MessageSerialization
{
    public class SystemTextJsonSerializationFactory : IMessageSerializationFactory
    {
        private readonly SystemTextJsonSerializer _serializer;

        public SystemTextJsonSerializationFactory()
            : this(null)
        {
        }

        public SystemTextJsonSerializationFactory(JsonSerializerOptions options)
        {
            _serializer = new SystemTextJsonSerializer(options);
        }

        public IMessageSerializer GetSerializer<T>() where T : class => _serializer;
    }
}
