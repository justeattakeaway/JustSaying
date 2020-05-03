using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace JustSaying.Messaging.MessageSerialization
{
    public class MessageSerializationRegister : IMessageSerializationRegister
    {
        private readonly IMessageSubjectProvider _messageSubjectProvider;
        private readonly IDictionary<Type, TypeSerializer> _map = new ConcurrentDictionary<Type, TypeSerializer>();

        public MessageSerializationRegister(IMessageSubjectProvider messageSubjectProvider)
        {
            _messageSubjectProvider = messageSubjectProvider ?? throw new ArgumentNullException(nameof(messageSubjectProvider));
        }

        public void AddSerializer<T>(IMessageSerializer serializer) where T : class
        {
            var key = typeof(T);
            if (!_map.TryGetValue(key, out TypeSerializer typeSerializer))
            {
                _map[key] = new TypeSerializer(typeof(T), serializer);
            }
        }

        public object DeserializeMessage(string body)
        {
            foreach (var pair in _map)
            {
                TypeSerializer typeSerializer = pair.Value;
                string messageSubject = typeSerializer.Serializer.GetMessageSubject(body);

                if (string.IsNullOrWhiteSpace(messageSubject))
                {
                    continue;
                }

                Type matchedType = typeSerializer.Type;

                if (!string.Equals(_messageSubjectProvider.GetSubjectForType(matchedType), messageSubject, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                IMessageSerializer messageSerializer = typeSerializer.Serializer;
                return messageSerializer.Deserialize(body, matchedType);
            }

            // TODO Maybe we should log the body separately (at debug/trace?), rather than include it in the exception message. Then they're easier to filter.
            throw new MessageFormatNotSupportedException(
                $"Message can not be handled - type undetermined. Message body: '{body}'");
        }

        public string Serialize<T>(T message, bool serializeForSnsPublishing)
            where T : class
        {
            var messageType = message.GetType();

            if (!_map.TryGetValue(messageType, out TypeSerializer typeSerializer))
            {
                // TODO Log out what *is* registered at debug?
                throw new MessageFormatNotSupportedException($"Failed to serialize message of type {messageType} because it is not registered for serialization.");
            }

            IMessageSerializer messageSerializer = typeSerializer.Serializer;
            return messageSerializer.Serialize(message, serializeForSnsPublishing, _messageSubjectProvider.GetSubjectForType(messageType));
        }
    }
}
