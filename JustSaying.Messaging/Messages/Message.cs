using System;
using Newtonsoft.Json;

namespace JustSaying.Messaging.Messages
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
        
        //footprint in order to avoid the same message being processed multiple times.
        public virtual string UniqueKey()
        {
            return Id.ToString();
        }
    }
}