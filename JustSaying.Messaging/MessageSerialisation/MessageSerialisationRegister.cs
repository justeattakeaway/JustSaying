using System;
using System.Collections.Generic;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public class MessageSerialisationRegister : IMessageSerialisationRegister
    {
        private readonly Dictionary<string, TypeSerialiser> _map;

        public MessageSerialisationRegister()
        {
            _map = new Dictionary<string, TypeSerialiser>();
        }

        public TypeSerialiser GeTypeSerialiser(string objectType)
        {
            return _map[objectType];
        }

        public TypeSerialiser GeTypeSerialiser(Type objectType)
        {
            return _map[objectType.Name];
        }

        public void AddSerialiser<T>(IMessageSerialiser serialiser) where T : Message
        {
            var keyname = typeof (T).Name;
            if (! _map.ContainsKey(keyname))
                _map.Add(keyname, new TypeSerialiser(typeof(T), serialiser));
        }
    }
}