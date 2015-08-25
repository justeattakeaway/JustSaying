using System;

namespace JustSaying.Messaging.Interrogation
{
    public class Publisher : IPublisher
    {
        public Publisher(Type messageType)
        {
            MessageType = messageType;
        }

        public Type MessageType { get; }
    }
}