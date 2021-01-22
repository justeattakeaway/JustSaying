using System;

namespace JustSaying.Models
{
    public abstract class Message
    {
        protected Message()
        {
            TimeStampWithOffset = DateTimeOffset.Now;
            TimeStamp = TimeStampWithOffset.UtcDateTime;            
            Id = Guid.NewGuid();            
        }

        public Guid Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTimeOffset TimeStampWithOffset { get; set; }
        public string RaisingComponent { get; set; }
        public string Version { get; private set; }
        public string SourceIp { get; private set; }
        public string Tenant { get; set; }
        public string Conversation { get; set; }

        //footprint in order to avoid the same message being processed multiple times.
        public virtual string UniqueKey() => Id.ToString();
    }
}
