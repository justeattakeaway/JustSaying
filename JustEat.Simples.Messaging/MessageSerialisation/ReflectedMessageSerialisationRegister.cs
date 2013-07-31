using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.Messaging.MessageSerialisation
{
    public class ReflectedMessageSerialisationRegister : IMessageSerialisationRegister
    {
        private readonly Dictionary<string, IMessageSerialiser<Message>> _map;

        public ReflectedMessageSerialisationRegister()
        {
            _map = new Dictionary<string, IMessageSerialiser<Message>>();

            var messageType = typeof (Message);

            var newtonsoftType = typeof (NewtonsoftSerialiser<>);

            Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.IsSubclassOf(messageType) && !x.IsAbstract)
                .ToList()
                .ForEach(x =>
                {
                    var genericType = newtonsoftType.MakeGenericType(new[] {x});
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
    }
}