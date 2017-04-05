using System;

namespace JustSaying.Messaging.MessageSerialisation
{
    public class TypeSerialiser
    {
        public Type Type { get; private set; }
        public IMessageSerialiser Serialiser { get; private set; }

        public TypeSerialiser(Type type, IMessageSerialiser serialiser)
        {
            Type = type;
            Serialiser = serialiser;
        }
    }
}