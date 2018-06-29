using System;
using System.Collections.Generic;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public class MessageSerialisationRegister : IMessageSerialisationRegister
    {
        private readonly IMessageSubjectProvider _messageSubjectProvider;
        private readonly Dictionary<Type, TypeSerialiser> _map = new Dictionary<Type, TypeSerialiser>();

        public MessageSerialisationRegister(IMessageSubjectProvider messageSubjectProvider)
        {
            _messageSubjectProvider = messageSubjectProvider;
        }

        public void AddSerialiser<T>(IMessageSerialiser serialiser) where T : Message
        {
            var key = typeof(T);
            if (!_map.ContainsKey(key))
            {
                _map.Add(key, new TypeSerialiser(typeof(T), serialiser));
            }
        }

        public Message DeserializeMessage(string body)
        {
            foreach (var formatter in _map)
            {
                var messageSubject = formatter.Value.Serialiser.GetMessageSubject(body);
                if (string.IsNullOrWhiteSpace(messageSubject))
                {
                    continue;
                }

                var matchedType = formatter.Value.Type;
                if (!string.Equals(_messageSubjectProvider.GetSubjectForType(matchedType), messageSubject, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                return formatter.Value.Serialiser.Deserialise(body, matchedType);
            }

            throw new MessageFormatNotSupportedException(
                $"Message can not be handled - type undetermined. Message body: '{body}'");
        }

        public string Serialise(Message message, bool serializeForSnsPublishing)
        {
            var messageType = message.GetType();
            var formatter = _map[messageType];
            return formatter.Serialiser.Serialise(message, serializeForSnsPublishing, _messageSubjectProvider.GetSubjectForType(messageType));
        }

    }
}
