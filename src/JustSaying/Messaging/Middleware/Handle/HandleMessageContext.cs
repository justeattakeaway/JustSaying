using System;
using JustSaying.Models;

namespace JustSaying.Messaging.Middleware.Handle
{
    public sealed class HandleMessageContext
    {
        private readonly Type _messageType;
        private readonly Message _message;

        public HandleMessageContext(Message message, Type messageType)
        {
            _message = message;
            _messageType = messageType;
        }

        public T Message<T> () where T : Message
        {
            if (typeof(T) != _messageType)
            {
                throw new InvalidOperationException($"This context has no message of type {_messageType.Name}");
            }

            return (T)_message;
        }

    }
}
