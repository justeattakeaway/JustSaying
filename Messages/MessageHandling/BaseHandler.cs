using System;
using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.Messaging.MessageHandling
{
    public abstract class BaseHandler<T> : IHandler<T> where T : Message
    {
        public Type HandlesMessageType { get { return typeof(T); } }
        public abstract bool Handle(Message message);
    }
}