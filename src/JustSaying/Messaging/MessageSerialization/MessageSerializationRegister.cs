using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialization
{
    public class MessageSerializationRegister : IMessageSerializationRegister
    {
        private readonly IMessageSubjectProvider _messageSubjectProvider;
        private readonly IMessageSerializationFactory _serializationFactory;
        private readonly IDictionary<Type, TypeSerializer> _map = new ConcurrentDictionary<Type, TypeSerializer>();

        public MessageSerializationRegister(IMessageSubjectProvider messageSubjectProvider, IMessageSerializationFactory serializationFactory)
        {
            _messageSubjectProvider = messageSubjectProvider ?? throw new ArgumentNullException(nameof(messageSubjectProvider));
            _serializationFactory = serializationFactory;
        }

        public void AddSerializer<T>() where T : Message
        {
            Type key = typeof(T);
            if (!_map.ContainsKey(key))
            {
                _map[key] = new TypeSerializer(typeof(T), _serializationFactory.GetSerializer<T>());
            }
        }

        public (Message, MessageAttributes) DeserializeMessage(string body)
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

                var attributes = typeSerializer.Serializer.GetMessageAttributes(body);
                var message = typeSerializer.Serializer.Deserialize(body, matchedType);
                return (message, attributes);
            }

            // TODO Maybe we should log the body separately (at debug/trace?), rather than include it in the exception message. Then they're easier to filter.
            throw new MessageFormatNotSupportedException(
                $"Message can not be handled - type undetermined. Message body: '{body}'");
        }

        public string Serialize(Message message, bool serializeForSnsPublishing)
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
