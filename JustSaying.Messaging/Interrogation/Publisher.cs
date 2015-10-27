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

        public override bool Equals(object obj)
        {
            return MessageType == ((Publisher)obj).MessageType;
        }

        public override int GetHashCode()
        {
            return (MessageType != null ? MessageType.GetHashCode() : 0);
        }
    }
}