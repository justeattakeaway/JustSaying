using System;

namespace JustSaying.Messaging.MessageHandling
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExactlyOnceAttribute : Attribute
    {
        public ExactlyOnceAttribute()
        {
            TimeOut = int.MaxValue;
        }
        public int TimeOut { get; set; }
    }
}