using System;
using System.Collections.Generic;
using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.Messaging.MessageSerialisation
{
    public class MessageSerialisationRegister : IMessageSerialisationRegister
    {
        private readonly Dictionary<string, IMessageSerialiser<Message>> _map;

        public MessageSerialisationRegister()
        {
            _map = new Dictionary<string, IMessageSerialiser<Message>>();
        }

        public IMessageSerialiser<Message> GetSerialiser(string objectType)
        {
            return _map[objectType];
        }

        public IMessageSerialiser<Message> GetSerialiser(Type objectType)
        {
            return _map[objectType.Name];
        }

        public void AddSerialiser<T>(IMessageSerialiser<Message> serialiser) where T : Message
        {
            var keyname = typeof (T).Name;
            if (! _map.ContainsKey(keyname))
                _map.Add(keyname, serialiser);
        }
    }
}