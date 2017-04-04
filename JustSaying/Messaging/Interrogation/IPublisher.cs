using System;

namespace JustSaying.Messaging.Interrogation
{
    public interface IPublisher
    {
        Type MessageType { get; set; }
    }
}