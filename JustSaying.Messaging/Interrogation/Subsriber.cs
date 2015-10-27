using System;

namespace JustSaying.Messaging.Interrogation
{
    public class Subsriber : ISubscriber
    {
        public Subsriber(Type messageType)
        {
            MessageType = messageType;
        }

        public Type MessageType { get; set; }

        public override bool Equals(object obj)
        {
            return MessageType == ((Subsriber)obj).MessageType;
        }

        public override int GetHashCode()
        {
            return (MessageType != null ? MessageType.GetHashCode() : 0);
        }
    }
}