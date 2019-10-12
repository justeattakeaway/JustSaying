using System;

namespace JustSaying.Messaging.Interrogation
{
    public class Subscriber : ISubscriber
    {
        public Subscriber(Type messageType)
        {
            MessageType = messageType;
        }

        public Type MessageType { get; set; }

        public override bool Equals(object obj) => MessageType == ((Subscriber)obj).MessageType;

        public override int GetHashCode() => MessageType?.GetHashCode() ?? 0;
    }
}