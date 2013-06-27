using System;

namespace SimplesNotificationStack.Messaging.Messages
{
    public abstract class Message
    {
        public Message()
        {
            TimeStamp = DateTime.UtcNow;
        }

        public DateTime TimeStamp { get; private set; }
        public Component RaisingComponent { get; private set; }
        public string Version{ get; private set; }
        public string SourceIp { get; private set; }
    }
}