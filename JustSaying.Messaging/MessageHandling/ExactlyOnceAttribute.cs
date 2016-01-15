using System;

namespace JustSaying.Messaging.MessageHandling
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExactlyOnceAttribute : Attribute
    {
        public ExactlyOnceAttribute()
        {
            TimeOut = (int) TimeSpan.MaxValue.TotalSeconds;
        }
        public int TimeOut { get; set; }
    }
}