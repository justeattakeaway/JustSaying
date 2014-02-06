using System;
using Newtonsoft.Json;

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
        public string RaisingComponent { get; set; }
        public string Version{ get; private set; }
        public string SourceIp { get; private set; }
        public string Tenant { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Conversation { get; set; }
    }
}