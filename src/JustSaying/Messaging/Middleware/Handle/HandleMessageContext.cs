using System;
using JustSaying.Models;

namespace JustSaying.Messaging.Middleware.Handle
{
    public sealed class HandleMessageContext
    {
        public HandleMessageContext(Message message, Type messageType, string queueName)
        {
            Message = message;
            MessageType = messageType;
            QueueName = queueName;
        }

        public string QueueName { get; }

        public Type MessageType { get; }

        public Message Message { get; }

        public TMessage MessageAs<TMessage>() where TMessage : Message => Message as TMessage;
    }
}
