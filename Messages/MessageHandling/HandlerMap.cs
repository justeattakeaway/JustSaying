using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Messaging.MessageHandling
{
    public interface IHandler<out T> where T : Message
    {
        Type HandlesMessageType { get; }
        bool Handle(Message message);
    }

    public abstract class BaseHandler<T> : IHandler<T> where T : Message
    {
        public Type HandlesMessageType { get { return typeof(T); } }
        public abstract bool Handle(Message message);
    }

    public interface IHandlerMap
    {
        void RegisterHandler(IHandler<Message> handler);
        IHandler<Message> GetHandler<T>(T message);
    }

    public class HandlerMap : IHandlerMap
    {
        private static readonly Dictionary<Type, IHandler<Message>> Handlers = new Dictionary<Type, IHandler<Message>>();

        public void RegisterHandler(IHandler<Message> handler)
        {
            Handlers.Add(handler.HandlesMessageType, handler);
        }

        public IHandler<Message> GetHandler<T>(T message)
        {
            return Handlers[typeof (T)];
        }
    }
}
