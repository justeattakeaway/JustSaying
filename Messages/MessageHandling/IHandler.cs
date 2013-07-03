using System;
using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.Messaging.MessageHandling
{
    public interface IHandler<out T> where T : Message
    {
        Type HandlesMessageType { get; }
        bool Handle(Message message);
    }
}
