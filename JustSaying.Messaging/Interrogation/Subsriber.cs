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
    }
}