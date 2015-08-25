using System;

namespace JustSaying.Messaging.Interrogation
{
    public interface ISubscriber
    {
        Type MessageType { get; }
    }
}