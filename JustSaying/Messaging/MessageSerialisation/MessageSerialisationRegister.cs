using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public class MessageSerialisationRegister : IMessageSerialisationRegister
    {
        // TODO Inconsistent use of American and British English for "serialize"/"serialise"

        private readonly IMessageSubjectProvider _messageSubjectProvider;
        private readonly IDictionary<Type, TypeSerialiser> _map = new ConcurrentDictionary<Type, TypeSerialiser>();

        public MessageSerialisationRegister(IMessageSubjectProvider messageSubjectProvider)
        {
            _messageSubjectProvider = messageSubjectProvider ?? throw new ArgumentNullException(nameof(messageSubjectProvider));
        }

        public void AddSerialiser<T>(IMessageSerialiser serialiser) where T : Message
        {
            var key = typeof(T);
            if (!_map.TryGetValue(key, out TypeSerialiser serializer))
            {
                _map[key] = new TypeSerialiser(typeof(T), serialiser);
            }
        }

        public Message DeserializeMessage(string body)
        {
            foreach (var pair in _map)
            {
                TypeSerialiser typeSerializer = pair.Value;
                string messageSubject = typeSerializer.Serialiser.GetMessageSubject(body);

                if (string.IsNullOrWhiteSpace(messageSubject))
                {
                    continue;
                }

                Type matchedType = typeSerializer.Type;

                if (!string.Equals(_messageSubjectProvider.GetSubjectForType(matchedType), messageSubject, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                IMessageSerialiser messageSerializer = typeSerializer.Serialiser;
                return messageSerializer.Deserialise(body, matchedType);
            }

            // TODO Maybe we should log the body separately (at debug/trace?), rather than include it in the exception message. Then they're easier to filter.
            throw new MessageFormatNotSupportedException(
                $"Message can not be handled - type undetermined. Message body: '{body}'");
        }

        public string Serialise(Message message, bool serializeForSnsPublishing)
        {
            var messageType = message.GetType();

            if (!_map.TryGetValue(messageType, out TypeSerialiser typeSerializer))
            {
                // TODO Log out what *is* registered at debug?
                throw new MessageFormatNotSupportedException($"Failed to serialize message of type {messageType} because it is not registered for serialization.");
            }

            IMessageSerialiser messageSerializer = typeSerializer.Serialiser;
            return messageSerializer.Serialise(message, serializeForSnsPublishing, _messageSubjectProvider.GetSubjectForType(messageType));
        }
    }
}
