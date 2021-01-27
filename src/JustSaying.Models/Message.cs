using System;

namespace JustSaying.Models
{
    public abstract class Message
    {
        protected Message()
        {
            TimeStamp = DateTime.UtcNow;            
            Id = Guid.NewGuid();            
        }

        public Guid Id { get; set; }
        public DateTime TimeStamp { get; set; }
        //NOTE: 
        //1. this is nullable because it is not present in older versions of JustSaying and we need versions to interop seamlessly
        //2. we do not set it in the constructor, since it would mean when consuming messages that don't have this property it gets set spuriously
        public DateTimeOffset? TimeStampWithOffset { get; set; }
        public string RaisingComponent { get; set; }
        public string Version { get; private set; }
        public string SourceIp { get; private set; }
        public string Tenant { get; set; }
        public string Conversation { get; set; }

        //footprint in order to avoid the same message being processed multiple times.
        public virtual string UniqueKey() => Id.ToString();
    }
}
