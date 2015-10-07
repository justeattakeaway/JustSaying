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
    }
}