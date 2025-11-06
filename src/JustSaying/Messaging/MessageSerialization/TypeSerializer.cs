using System;

namespace JustSaying.Messaging.MessageSerialization
{
    public class TypeSerializer
    {
        public Type Type { get; private set; }
        public IMessageSerializer Serializer { get; private set; }

        public TypeSerializer(Type type, IMessageSerializer serializer)
        {
            Type = type;
            Serializer = serializer;
        }
    }
}
