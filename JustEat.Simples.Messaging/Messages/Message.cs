using System;

namespace JustEat.Simples.NotificationStack.Messaging.Messages
{
    public abstract class Message
    {
        public Message()
        {
            TimeStamp = DateTime.UtcNow;
        }

        public Guid Id { get; set; }
        public DateTime TimeStamp { get; private set; }
        public Component RaisingComponent { get; set; }
        public string Version{ get; private set; }
        public string SourceIp { get; private set; }
    }
}