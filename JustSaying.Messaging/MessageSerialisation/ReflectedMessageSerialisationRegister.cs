using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    [Obsolete("No longer loading messages via reflection.", true)]
    public class ReflectedMessageSerialisationRegister : IMessageSerialisationRegister
    {
        private readonly Type _serialiserType = typeof(ServiceStackSerialiser<>); // ToDo: This should be passed in to the fluent stack so that consumers can choose!
        private readonly Dictionary<string, IMessageSerialiser<Message>> _map;

        public ReflectedMessageSerialisationRegister()
        {
            _map = new Dictionary<string, IMessageSerialiser<Message>>();

            var messageType = typeof (Message);

            Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.IsSubclassOf(messageType) && !x.IsAbstract)
                .ToList()
                .ForEach(x =>
                {
                    var genericType = _serialiserType.MakeGenericType(new[] {x});
                    _map.Add(x.Name, (IMessageSerialiser<Message>) Activator.CreateInstance(genericType));
                });
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
            // I don't care about this as I already have it all thank you very much.
        }
    }
}