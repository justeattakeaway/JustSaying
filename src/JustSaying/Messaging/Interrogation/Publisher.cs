using System;

namespace JustSaying.Messaging.Interrogation
{
    public class Publisher : IPublisher
    {
        public Publisher(Type messageType)
        {
            MessageType = messageType;
        }

        public Type MessageType { get; set; }

        public override bool Equals(object obj) => MessageType == ((Publisher)obj).MessageType;

        public override int GetHashCode() => MessageType?.GetHashCode() ?? 0;
    }
}