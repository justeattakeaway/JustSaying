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
            foreach (var formatter in _map)
            {
                string messageSubject = formatter.Value.Serialiser.GetMessageSubject(body);

                if (string.IsNullOrWhiteSpace(messageSubject))
                {
                    continue;
                }

                Type matchedType = formatter.Value.Type;

                if (!string.Equals(_messageSubjectProvider.GetSubjectForType(matchedType), messageSubject, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return formatter.Value.Serialiser.Deserialise(body, matchedType);
            }

            // TODO Maybe we should log the body separately (at debug/trace?), rather than include it in the exception message. Then they're easier to filter.
            throw new MessageFormatNotSupportedException(
                $"Message can not be handled - type undetermined. Message body: '{body}'");
        }

        public string Serialise(Message message, bool serializeForSnsPublishing)
        {
            var messageType = message.GetType();

            if (!_map.TryGetValue(messageType, out TypeSerialiser serializer))
            {
                // TODO Log out what *is* registered at debug?
                throw new MessageFormatNotSupportedException($"Failed to serialize message of type {messageType} because it is not registered for serialization.");
            }

            return serializer.Serialiser.Serialise(message, serializeForSnsPublishing, _messageSubjectProvider.GetSubjectForType(messageType));
        }
    }
}
